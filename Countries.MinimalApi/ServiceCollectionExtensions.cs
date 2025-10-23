using System.Net;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Countries.Business.Services;
using Countries.Domain.Channels;
using Countries.Domain.Enum;
using Countries.Domain.Repositories;
using Countries.Domain.Services;
using Countries.Infrastructure;
using Countries.Infrastructure.Database;
using Countries.Infrastructure.Repositories;
using Countries.MinimalApi.Channels;
using Countries.MinimalApi.ExceptionHandlers;
using Countries.MinimalApi.Healthchecks;
using Countries.MinimalApi.Identity;
using Countries.MinimalApi.Mapping;
using Countries.MinimalApi.Mapping.Interfaces;
using Countries.MinimalApi.Resiliency.Http;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Refit;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Countries.MinimalApi;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
    {
        var dbConnection1 = configuration.GetValue<string>("Db1");
        var dbConnection2 = configuration.GetValue<string>("Db2");
        var dbConnection3 = configuration.GetConnectionString("Db3");

        services.AddDbContextPool<DatabaseContext>(options =>
            options.UseSqlServer(dbConnection3));

        services.AddHealthChecks()
            .AddSqlServer(name: "SQL1", connectionString: dbConnection1, tags: new[] { "live" })
            .AddSqlServer(name: "SQL2", connectionString: dbConnection2, tags: new[] { "live" })
            .AddCheck<ReadyHealthCheck>("Readiness check", tags: new[] { "ready" });

        return services;
    }

    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IUserProfile, UserProfile>();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.Authority = "https://login.microsoftonline.com/136544d9-038e-4646-afff-10accb370679/v2.0";
            options.Audience = "257b6c36-1168-4aac-be93-6f2cd81cec43";
            options.TokenValidationParameters.ValidateLifetime = true;
            options.TokenValidationParameters.ValidateIssuer = true;
            options.TokenValidationParameters.ClockSkew = TimeSpan.FromMinutes(5);
        });

        services.AddAuthorization(options =>
            options.AddPolicy("SurveyCreator",
                policy => policy.RequireRole("SurveyCreator")
            ));

        return services;
    }

    public static IServiceCollection AddSwaggerServices(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Countries API",
                Version = "1.0",
                Description = "Version 1.0 of the Countries API"
            });

            c.SwaggerDoc("v2", new OpenApiInfo
            {
                Title = "Countries API",
                Version = "2.0",
                Description = "Version 2.0 of the Countries API"
            });

            // Add JWT Bearer authentication
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description =
                    "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\""
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            c.DocInclusionPredicate((docName, apiDesc) =>
            {
                if (apiDesc.GroupName == null)
                    return false;

                return apiDesc.GroupName == docName;
            });

            c.CustomOperationIds(apiDesc =>
            {
                return apiDesc.TryGetMethodInfo(out var methodInfo)
                    ? $"{methodInfo.DeclaringType?.Name}_{methodInfo.Name}"
                    : null;
            });

            var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
        });

        return services;
    }

    public static IServiceCollection AddApiVersioningServices(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ReportApiVersions = true;
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ApiVersionReader = ApiVersionReader.Combine(
                    new HeaderApiVersionReader("api-version"),
                    new QueryStringApiVersionReader("api-version")
                );
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'V";
                options.SubstituteApiVersionInUrl = true;
            });

        return services;
    }

    public static IServiceCollection AddRateLimitingServices(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = (int)HttpStatusCode.TooManyRequests;
            options.OnRejected = async (context, token) =>
            {
                await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.");
            };

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var priceTierService = httpContext.RequestServices.GetRequiredService<IPricingTierService>();
                var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var priceTier = priceTierService.GetPricingTier(ip);

                return priceTier switch
                {
                    PricingTier.Paid => RateLimitPartition.GetFixedWindowLimiter(
                        ip,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            QueueLimit = 10,
                            PermitLimit = 50,
                            Window = TimeSpan.FromSeconds(15)
                        }),
                    PricingTier.Free => RateLimitPartition.GetFixedWindowLimiter(
                        ip,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 1,
                            Window = TimeSpan.FromSeconds(15)
                        }),
                    _ => RateLimitPartition.GetFixedWindowLimiter(
                        ip,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 1,
                            Window = TimeSpan.FromSeconds(15)
                        })
                };
            });

            options.AddPolicy("ShortLimit", context =>
            {
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(ip,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 2,
                        Window = TimeSpan.FromSeconds(15)
                    });
            });
        });

        return services;
    }

    public static IServiceCollection AddCachingServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("RedisConnectionString");
            options.InstanceName = "Demo";
        });

        services.AddOutputCache(options =>
        {
            options.AddPolicy("5minutes", builder =>
                builder.Expire(TimeSpan.FromSeconds(300))
                    .SetVaryByQuery("*")
            );
        });

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<Program>();

        // Repositories
        services.AddScoped<ICountryRepository, CountryRepository>();

        // Mappers
        services.AddScoped<ICountryMapper, CountryMapper>();

        // Services (base service first, then decorators)
        services.AddScoped<ICountryService, CountryService>();
        services.Decorate<ICountryService, CachedCountryService>();
        services.Decorate<ICountryService, DistributedCachedCountryService>();

        services.AddScoped<IPricingTierService, PricingTierService>();
        services.AddScoped<IStreamingService, StreamingService>();

        // Channels
        services.AddSingleton<ICountryFileIntegrationChannel, CountryFileIntegrationChannel>();
        services.AddHostedService<CountryFileIntegrationBackgroudService>();

        return services;
    }

    public static IServiceCollection AddExternalServices(this IServiceCollection services)
    {
        services.AddRefitClient<IMediaRepository>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://anthonygiretti.blob.core.windows.net"))
            .AddFaultHandlingPolicy();

        services.AddHttpClient();

        return services;
    }

    public static IServiceCollection AddExceptionHandlingServices(this IServiceCollection services)
    {
        services.AddExceptionHandler<TimeOutExceptionHandler>();
        services.AddExceptionHandler<DefaultExceptionHandler>();

        return services;
    }

    public static IServiceCollection AddCorsServices(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll",
                builder =>
                {
                    builder.AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowAnyOrigin();
                });

            options.AddPolicy("Restricted",
                builder =>
                {
                    builder.AllowAnyHeader()
                        .WithMethods("GET", "POST", "PUT", "DELETE")
                        .WithOrigins("https://mydomain.com", "https://myotherdomain.com")
                        .AllowCredentials();
                });
        });

        return services;
    }

    public static IServiceCollection AddHostConfiguration(this IServiceCollection services)
    {
        services.AddAntiforgery();

        services.PostConfigure<HostOptions>(option => { option.ShutdownTimeout = TimeSpan.FromSeconds(60); });

        services.AddApplicationInsightsTelemetry();

        return services;
    }
}