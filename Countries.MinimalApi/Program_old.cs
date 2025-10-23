// using System.Net;
// using System.Threading.RateLimiting;
// using Asp.Versioning;
// using Asp.Versioning.ApiExplorer;
// using Azure.Identity;
// using Countries.Business.Services;
// using Countries.Domain.Channels;
// using Countries.Domain.DTOs;
// using Countries.Domain.Enum;
// using Countries.Domain.Repositories;
// using Countries.Domain.Services;
// using Countries.Infrastructure;
// using Countries.Infrastructure.Database;
// using Countries.Infrastructure.Repositories;
// using Countries.MinimalApi.Channels;
// using Countries.MinimalApi.EndpointFilters;
// using Countries.MinimalApi.Endpoints;
// using Countries.MinimalApi.ExceptionHandlers;
// using Countries.MinimalApi.Healthchecks;
// using Countries.MinimalApi.Identity;
// using Countries.MinimalApi.Mapping;
// using Countries.MinimalApi.Mapping.Interfaces;
// using Countries.MinimalApi.Models;
// using Countries.MinimalApi.Resiliency.Http;
// using Countries.MinimalApi.RouteGroups;
// using Countries.MinimalApi.Swagger;
// using FluentValidation;
// using Microsoft.AspNetCore.Authentication.JwtBearer;
// using Microsoft.AspNetCore.Diagnostics.HealthChecks;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.Options;
// using Microsoft.OpenApi.Models;
// using Refit;
// using Serilog;
// using Swashbuckle.AspNetCore.SwaggerGen;
//
// var builder = WebApplication.CreateBuilder(args);
//
// builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));
//
// var keyVaultUri = builder.Configuration.GetValue<string>("KeyVault:Uri");
// builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential());
//
// var dbConnection1 = builder.Configuration.GetValue<string>("Db1");
// var dbConnection2 = builder.Configuration.GetValue<string>("Db2");
// var dbConnection3 = builder.Configuration.GetConnectionString("Db3");
// builder.Services.AddDbContextPool<DatabaseContext>(options =>
//     options.UseSqlServer(dbConnection3));
// // builder.Services.AddDbContextPool<DatabaseContext>(options => 
// //     options.UseSqlServer(dbConnection1, 
// //         sqlServerOptionsAction: sqlOptions =>
// //         {
// //             sqlOptions.EnableRetryOnFailure(
// //                 maxRetryCount: 3);
// //         }));
//
// builder.Services.AddHttpContextAccessor();
// builder.Services.AddScoped<IUserProfile, UserProfile>();
//
// builder.Services.AddAuthentication(options =>
// {
//     options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//     options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
// }).AddJwtBearer(options =>
// {
//     options.Authority = "https://login.microsoftonline.com/136544d9-038e-4646-afff-10accb370679/v2.0";
//     options.Audience = "257b6c36-1168-4aac-be93-6f2cd81cec43";
//     options.TokenValidationParameters.ValidateLifetime = true;
//     options.TokenValidationParameters.ValidateIssuer = true;
//     options.TokenValidationParameters.ClockSkew = TimeSpan.FromMinutes(5);
// });
//
// builder.Services
//     .AddAuthorization(options =>
//         options.AddPolicy("SurveyCreator",
//             policy => policy.RequireRole("SurveyCreator")
//         ));
//
// builder.Services.AddEndpointsApiExplorer();
//
// builder.Services.AddApiVersioning(options =>
//     {
//         options.DefaultApiVersion = new ApiVersion(1, 0);
//         options.ReportApiVersions = true;
//         options.AssumeDefaultVersionWhenUnspecified = true;
//         options.ApiVersionReader = new HeaderApiVersionReader("api-version");
//     })
//     .AddApiExplorer(options =>
//     {
//         options.GroupNameFormat = "'v'VV"; //format the version as "'v'major[.minor]"
//     });
//
// // builder.Services.AddSwaggerGen(c =>
// // {
// //     c.SwaggerDoc("v1.0", new OpenApiInfo { Title = "Minimal API"});
// // });
//
// // builder.Services.AddSwaggerGen(options =>
// // {
// //     options.EnableAnnotations();
// //     options.AddXmlComments();
// // });
// builder.Services.AddSingleton<IConfigureOptions<SwaggerGenOptions>, SwaggerConfigurationsOptions>();
// builder.Services.AddAntiforgery();
//
//
// builder.Services.AddSwaggerGen(c =>
// {
//     c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
//     {
//         Name = "Authorization",
//         Scheme = "Bearer",
//         BearerFormat = "JWT",
//         In = ParameterLocation.Header,
//         Description = "JWT Authorization header required"
//     });
//     c.AddSecurityRequirement(new OpenApiSecurityRequirement
//     {
//         {
//             new OpenApiSecurityScheme
//             {
//                 Reference = new OpenApiReference
//                 {
//                     Type = ReferenceType.SecurityScheme,
//                     Id = "Bearer"
//                 }
//             },
//             new string[] { }
//         }
//     });
// });
//
// builder.Services.AddRateLimiter(options =>
// {
//     options.RejectionStatusCode = (int)HttpStatusCode.TooManyRequests;
//     options.OnRejected = async (context, token) =>
//     {
//         await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.");
//     };
//     options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
//     {
//         var priceTierService = httpContext.RequestServices.GetRequiredService<IPricingTierService>();
//         var ip = httpContext.Connection.RemoteIpAddress.ToString();
//         var priceTier = priceTierService.GetPricingTier(ip);
//
//         return priceTier switch
//         {
//             PricingTier.Paid => RateLimitPartition.GetFixedWindowLimiter(
//                 ip,
//                 _ => new FixedWindowRateLimiterOptions
//                 {
//                     QueueLimit = 10,
//                     PermitLimit = 50,
//                     Window = TimeSpan.FromSeconds(15)
//                 }),
//             PricingTier.Free => RateLimitPartition.GetFixedWindowLimiter(
//                 ip,
//                 _ => new FixedWindowRateLimiterOptions
//                 {
//                     PermitLimit = 1,
//                     Window = TimeSpan.FromSeconds(15)
//                 })
//         };
//     });
//     /*
//     options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
//     {
//         var priceTierService = httpContext.RequestServices.GetRequiredService<IPricingTierService>();
//         var ip = httpContext.Connection.RemoteIpAddress.ToString();
//         var priceTier = priceTierService.GetPricingTier(ip);
//
//         return priceTier switch
//         {
//             PricingTier.Paid => RateLimitPartition.GetSlidingWindowLimiter(
//                 ip,
//                 _ => new SlidingWindowRateLimiterOptions
//                 {
//                     QueueLimit = 10,
//                     PermitLimit = 50,
//                     SegmentsPerWindow = 2,
//                     Window = TimeSpan.FromSeconds(15)
//                 }),
//             PricingTier.Free => RateLimitPartition.GetSlidingWindowLimiter(
//                 ip,
//                 _ => new SlidingWindowRateLimiterOptions
//                 {
//                     PermitLimit = 2,
//                     SegmentsPerWindow = 2,
//                     Window = TimeSpan.FromSeconds(15)
//                 })
//         };
//     });
//     */
//
//     /*
//     options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
//     {
//         var priceTierService = httpContext.RequestServices.GetRequiredService<IPricingTierService>();
//         var ip = httpContext.Connection.RemoteIpAddress.ToString();
//         var priceTier = priceTierService.GetPricingTier(ip);
//
//         return priceTier switch
//         {
//             PricingTier.Paid => RateLimitPartition.GetTokenBucketLimiter(
//                 ip,
//                 _ => new TokenBucketRateLimiterOptions
//                 {
//                     TokenLimit = 50,
//                     TokensPerPeriod = 25,
//                     ReplenishmentPeriod = TimeSpan.FromSeconds(15)
//                 }),
//             PricingTier.Free => RateLimitPartition.GetTokenBucketLimiter(
//                 ip,
//                 _ => new TokenBucketRateLimiterOptions
//                 {
//                     TokenLimit = 10,
//                     TokensPerPeriod = 5,
//                     ReplenishmentPeriod = TimeSpan.FromSeconds(15)
//                 })
//         };
//     });
//     */
//
//     /*
//     options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
//     {
//         var priceTierService = httpContext.RequestServices.GetRequiredService<IPricingTierService>();
//         var ip = httpContext.Connection.RemoteIpAddress.ToString();
//         var priceTier = priceTierService.GetPricingTier(ip);
//
//         return priceTier switch
//         {
//             PricingTier.Paid => RateLimitPartition.GetConcurrencyLimiter(
//                 ip,
//                 _ => new ConcurrencyLimiterOptions
//                 {
//                     QueueLimit = 10,
//                     PermitLimit = 50
//                 }),
//             PricingTier.Free => RateLimitPartition.GetConcurrencyLimiter(
//                 ip,
//                 _ => new ConcurrencyLimiterOptions
//                 {
//                     QueueLimit = 0,
//                     PermitLimit = 10
//                 })
//         };
//     });
//     */
//
//     options.AddPolicy("ShortLimit", context =>
//     {
//         var ip = context.Connection.RemoteIpAddress.ToString();
//         return RateLimitPartition.GetFixedWindowLimiter(ip,
//             _ => new FixedWindowRateLimiterOptions
//             {
//                 PermitLimit = 2,
//                 Window = TimeSpan.FromSeconds(15)
//             });
//     });
// });
//
// var dbConnection = builder.Configuration.GetValue<string>("ConnectionStrings:Db");
//
// builder.Services.AddDbContextPool<DatabaseContext>(options => options.UseSqlServer(dbConnection));
//
// builder.Services.AddHealthChecks()
//     .AddSqlServer(name: "SQL1", connectionString: dbConnection1, tags: new[] { "live" })
//     .AddSqlServer(name: "SQL2", connectionString: dbConnection2, tags: new[] { "live" })
//     .AddCheck<ReadyHealthCheck>("Readiness check", tags: new[] { "ready" });
//
// builder.Services.AddExceptionHandler<DefaultExceptionHandler>();
//
// builder.Services.AddValidatorsFromAssemblyContaining<Program>();
// builder.Services.Decorate<ICountryService, CachedCountryService>();
// builder.Services.Decorate<ICountryService, DistributedCachedCountryService>();
// builder.Services.AddRefitClient<IMediaRepository>()
//     .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://anthonygiretti.blob.core.windows.net"))
//     .AddFaultHandlingPolicy();
// builder.Services.AddScoped<ICountryRepository, CountryRepository>();
// builder.Services.AddScoped<ICountryMapper, CountryMapper>();
// builder.Services.AddScoped<ICountryService, CountryService>();
// builder.Services.AddScoped<IPricingTierService, PricingTierService>();
// builder.Services.AddScoped<IStreamingService, StreamingService>();
//
// builder.Services.AddSingleton<ICountryFileIntegrationChannel, CountryFileIntegrationChannel>();
// builder.Services.AddHostedService<CountryFileIntegrationBackgroudService>();
//
// builder.Services.AddExceptionHandler<TimeOutExceptionHandler>();
// builder.Services.AddExceptionHandler<DefaultExceptionHandler>();
//
// builder.Services.AddHttpClient();
//
// builder.Services.AddCors(options =>
// {
//     options.AddPolicy("AllowAll",
//         builder =>
//         {
//             builder.AllowAnyHeader()
//                 .AllowAnyMethod()
//                 .AllowAnyOrigin();
//         });
//
//     options.AddPolicy("Restricted",
//         builder =>
//         {
//             builder.AllowAnyHeader()
//                 .WithMethods("GET", "POST", "PUT", "DELETE")
//                 .WithOrigins("https://mydomain.com", "https://myotherdomain.com")
//                 .AllowCredentials();
//         });
// });
//
//
// // Allow up to 60 seconds to complete any in-progress results processing.
// builder.Services.PostConfigure<HostOptions>(option => { option.ShutdownTimeout = TimeSpan.FromSeconds(60); });
//
// builder.Services.AddOutputCache(options =>
// {
//     //options.AddBasePolicy(builder =>
//     //    builder.Expire(TimeSpan.FromSeconds(30)));
//     options.AddPolicy("5minutes", builder =>
//         builder.Expire(TimeSpan.FromSeconds(300))
//             .SetVaryByQuery("*")
//     );
// });
//
// builder.Services.AddMemoryCache();
//
// builder.Services.AddStackExchangeRedisCache(options =>
// {
//     options.Configuration = builder.Configuration.GetConnectionString("RedisConnectionString");
//     options.InstanceName = "Demo";
// });
//
// builder.Services.AddApplicationInsightsTelemetry();
//
// var app = builder.Build();
//
//
// var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
// app.UseSwagger().UseSwaggerUI(c =>
// {
//     c.SwaggerEndpoint("/swagger/v1.0/swagger.json", "Version 1.0");
//     c.SwaggerEndpoint("/swagger/v2.0/swagger.json", "Version 2.0");
//     //foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions.Reverse())
//     //{
//     //    c.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json",
//     //        description.GroupName.ToUpperInvariant());
//     //}
// });
//
//
// app.UseCors("Restricted");
//
// app.UseOutputCache();
//
// app.UseSwagger();
// // app.UseSwagger().UseSwaggerUI(c =>
// // {
// //     c.SwaggerEndpoint($"/swagger/v1.0/swagger.json", "Version 1.0");
// // });
// app.UseSwaggerUI();
//
// app.UseAuthentication();
// app.UseAuthorization();
//
// app.UseExceptionHandler(opt => { });
//
// app.UseRateLimiter();
//
// app.MapHealthChecks("/ready", new HealthCheckOptions
// {
//     Predicate = healthCheck => healthCheck.Tags.Contains("ready")
// });
//
// app.MapHealthChecks("/live", new HealthCheckOptions
// {
//     Predicate = healthCheck => healthCheck.Tags.Contains("live")
// });
//
// app.MapGet("/logging", (ILogger<Program> logger) =>
// {
//     logger.LogInformation("/logging endpoint has been invoked.");
//     return Results.Ok();
// });
//
// app.MapGet("/authenticated", () => { return Results.Ok("Authenticated !"); }).RequireAuthorization();
//
// app.MapGet("/authorized", (IUserProfile userProfile) => { return Results.Ok("Authorized !"); })
//     .RequireAuthorization("SurveyCreator");
//
// app.MapGet("/countries", CountryEndpoints.GetCountries);
//
// // app.MapGet("/countries", async (int? pageIndex, int? pageSize, ICountryMapper mapper, ICountryService countryService, ILogger<Program> logger) => {
// //
// //     var paging = new PagingDto
// //     {
// //         PageIndex = pageIndex.HasValue ? pageIndex.Value : 1,
// //         PageSize = pageSize.HasValue ? pageSize.Value : 10
// //     };
// //     var countries = await countryService.GetAllAsync(paging);
// //
// //     using (logger.BeginScope("Getting countries with page index {pageIndex} and page size {pageSize}", paging.PageIndex, paging.PageSize))
// //     {
// //         logger.LogInformation("Received {count} countries from the query", countries.Count);
// //         return Results.Ok(mapper.Map(countries));
// //     }
// // });
//
// // app.MapGet("/countries", async ([AsParameters] Paging paging, ICountryMapper mapper, ICountryService countryService) => {
// //     async IAsyncEnumerable<Country> StreamCountriesAsync()
// //     {
// //         var countries = await countryService.GetAllAsync(new PagingDto
// //         {
// //             PageIndex = paging.PageIndex.HasValue ? paging.PageIndex.Value : 1,
// //             PageSize = paging.PageSize.HasValue ? paging.PageSize.Value : 10
// //         });
// //         var mappedCountries = mapper.Map(countries);
// //         foreach (var country in mappedCountries)
// //         {
// //             yield return country;
// //         }
// //     }
// //     return StreamCountriesAsync();
// // });
//
// app.MapGet("/cachedcountries",
//     async ([AsParameters] Paging paging, ICountryMapper mapper, ICountryService countryService) =>
//     {
//         var countries = await countryService.GetAllAsync(new PagingDto
//         {
//             PageIndex = 1,
//             PageSize = 10
//         });
//         return Results.Ok(mapper.Map(countries));
//     }).CacheOutput("5minutes");
//
// app.MapGet("/cachedinmemorycountries", async (ICountryMapper mapper, ICountryService countryService) =>
// {
//     var countries = await countryService.GetAllAsync(new PagingDto
//     {
//         PageIndex = 1,
//         PageSize = 10
//     });
//     return Results.Ok(mapper.Map(countries));
// });
//
// app.MapPost("/countries/upload",
//     async (IFormFile file, ICountryFileIntegrationChannel channel, CancellationToken cancellationToken) =>
//     {
//         if (await channel.SubmitAsync(file.OpenReadStream(), cancellationToken))
//             Results.Accepted();
//         Results.StatusCode(StatusCodes.Status500InternalServerError);
//     }).DisableAntiforgery();
//
// app.MapGet("/cancellable", async (ICountryService countryService, CancellationToken cancellationToken) =>
// {
//     await countryService.LongRunningQueryAsync(cancellationToken);
//     return Results.Ok();
// });
//
// app.MapPost("/countries", async ([FromBody] Country country, ICountryMapper mapper, ICountryService countryService) =>
// {
//     var countryDto = mapper.Map(country);
//     var countryId = await countryService.CreateOrUpdateAsync(countryDto);
//     if (countryId <= 0)
//         return Results.StatusCode(StatusCodes.Status500InternalServerError);
//     return Results.CreatedAtRoute("countryById", new { Id = countryId });
// }).AddEndpointFilter<InputValidatorFilter<Country>>();
//
// app.MapGet("/countries/{id}", async (int id, ICountryMapper mapper, ICountryService countryService) =>
// {
//     var country = await countryService.RetrieveAsync(id);
//
//     if (country is null)
//         return Results.NotFound();
//
//     return Results.Ok(mapper.Map(country));
// }).WithName("countryById");
//
// app.MapGet("/countries", async (ICountryMapper mapper, ICountryService countryService) =>
// {
//     var countries = await countryService.GetAllAsync();
//     return Results.Ok(mapper.Map(countries));
// });
//
// app.MapDelete("/countries/{id}", async (int id, ICountryService countryService) =>
// {
//     if (await countryService.DeleteAsync(id))
//         return Results.NoContent();
//
//     return Results.NotFound();
// });
//
// app.MapPut("/countries", async ([FromBody] Country country, ICountryMapper mapper, ICountryService countryService) =>
// {
//     var countryDto = mapper.Map(country);
//     var countryId = await countryService.CreateOrUpdateAsync(countryDto);
//     if (countryId <= 0)
//         return Results.StatusCode(StatusCodes.Status500InternalServerError);
//
//     if (country.Id is null)
//         return Results.CreatedAtRoute("countryById", new { Id = countryId });
//     return Results.NoContent();
// }).AddEndpointFilter<InputValidatorFilter<Country>>();
//
// app.MapPatch("/countries/{id}",
//     async (int id, [FromBody] CountryPatch countryPatch, ICountryMapper mapper, ICountryService countryService) =>
//     {
//         if (await countryService.UpdateDescriptionAsync(id, countryPatch.Description))
//             return Results.NoContent();
//
//         return Results.NotFound();
//     }).AddEndpointFilter<InputValidatorFilter<CountryPatch>>();
//
// // using (var scope = app.Services.CreateScope())
// // {
// //     var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
// //     db.Database.SetConnectionString(dbConnection);
// //     db.Database.Migrate();
// // }
//
// /*
// using (var client = new HttpClient())
// {
//     byte[] fileBytes = await client.GetByteArrayAsync("https://anthonygiretti.blob.core.windows.net/countryflags/ca.png");
// }
// */
//
// /* Examples Route groups */
// app.AddCountryEndpoints();
// /* Examples Route groups */
//
// /* Examples custom binding */
// app.MapPost("/countries/upload", (IFormFile file, Country country) => { Results.NoContent(); }).DisableAntiforgery();
//
// app.MapGet("/countries/ids", ([FromHeader] CountryIds ids) => { Results.NoContent(); });
// /* Examples custom binding */
//
// /* Examples middlewares */
// app.Use(async (context, next) =>
// {
//     //app.Logger.LogInformation("Middleware 1 executed");
//     await next();
// }); // Don't stop the execution
//
// app.MapGet("/test", () =>
// {
//     //app.Logger.LogInformation("Endpoint GET /test has been invoked");
//     return Results.Ok();
// });
//
// //app.UseMiddleware<LoggingMiddleware>();// Doesn't stop the execution
//
// app.UseWhen(ctx => !string.IsNullOrEmpty(ctx.Request.Query["p"].ToString()),
//     builder =>
//     {
//         builder.Use(async (context, next) =>
//         {
//             //app.Logger.LogInformation("Nested middleware executed");
//             await next();
//         });
//
//         builder.Run(async context =>
//         {
//             //app.Logger.LogInformation("End of the pipeline end");
//             await Task.CompletedTask;
//         });
//     }); // Stops the execution if the condition is met because the UseWhen contains Run Middleware
//
// app.MapWhen(ctx => !string.IsNullOrEmpty(ctx.Request.Query["q"].ToString()),
//     builder =>
//     {
//         builder.Use(async (context, next) =>
//         {
//             //app.Logger.LogInformation("New middleware pipeline branch has been initiated");
//             await next();
//         });
//         builder.Run(async context =>
//         {
//             //app.Logger.LogInformation("New middleware pipeline will end here");
//             await Task.CompletedTask;
//         });
//     }); // Stops the execution if the condition is met because the new branch contains Run Middleware
//
// /*
// app.Map(new PathString("/test"),
// builder =>
// {
//     builder.Use(async (context, next) =>
//     {
//         app.Logger.LogInformation("New middleware pipeline branch has been initiated");
//         await next();
//     });
//     builder.Run(async context =>
//     {
//         app.Logger.LogInformation("New middleware pipeline will end here");
//         await Task.CompletedTask;
//     });
// });// stop execution
// */
// /* Examples middlewares */
//
// /* Examples Endpoint Filter */
// app.MapGet("/longrunning", async () =>
// {
//     await Task.Delay(5000);
//     return Results.Ok();
// }).AddEndpointFilter<LogPerformanceFilter>();
//
// app.MapPost("/countries",
//         ([FromBody] Country country) => { return Results.CreatedAtRoute("countryById", new { Id = 1 }); })
//     .AddEndpointFilter<InputValidatorFilter<Country>>();
// /* Examples Endpoint Filter */
//
// /* Examples RateLimiting */
// app.MapGet("/notlimited", () => { return Results.Ok(); }).DisableRateLimiting();
//
// app.MapGet("/limited", () => { return Results.Ok(); }).RequireRateLimiting("ShortLimit");
// /* Examples RateLimiting */
//
// /* Examples Exception handling */
// app.MapGet("/exception", () => { throw new Exception(); });
//
// app.MapGet("/timeout", () => { throw new TimeoutException(); });
//
// /* routing examples */
// //app.MapGet("/countries/{countryId}", (int countryId) => $"CountryId {countryId}");
//
// //app.MapGet("/countries", () => new List<string> { "France", "Canada", "Italy" });
//
// app.MapGet("/date/{date}", (DateTime date) => date.ToString());
//
// app.MapGet("/uniqueidentifier/{id}", (Guid id) => id.ToString());
//
// app.MapMethods("/users/{userId}", new List<string> { "PUT", "PATCH" }, (HttpRequest request) =>
// {
//     var id = request.RouteValues["id"];
//     var lastActivityDate = request.Form["lastactivitydate"];
// });
//
// app.MapMethods("/routeName", new List<string> { "OPTIONS", "HEAD", "TRACE" }, () =>
// {
//     // Do action
// });
//
//
// app.MapGet("/provinces/{provinceId:int:max(12)}", (int provinceId) => $"ProvinceId {provinceId}");
//
//
// /* routing examples */
//
// /* Route groups examples */
// //app.MapGroup("/countries").GroupCountries();
// /* Route groups examples */
//
// /* Parameter binding examples */
// app.MapPost("/Addresses", ([FromBody] Address address) => { return Results.Created(); });
//
// app.MapPut("/Addresses/{addressId}",
//     ([FromRoute] int addressId, [FromForm] Address address) => { return Results.NoContent(); }).DisableAntiforgery();
//
// app.MapGet("/Addresses",
//     ([FromHeader] string coordinates, [FromQuery] int? limitCountSearch) => { return Results.Ok(); });
//
// app.MapGet("/IdList", ([FromQuery] int[] id) => { return Results.Ok(); });
//
// app.MapGet("/languages", ([FromHeader(Name = "lng")] string[] lng) => { return Results.Ok(lng); });
//
// /* Parameter binding examples */
//
// /* Validation examples & CRUD examples */
//
// // app.MapPost("/countries",
// //     ([FromBody] Country country, IValidator<Country> validator, ICountryMapper mapper,
// //         ICountryService countryService) =>
// //     {
// //         var validationResult = validator.Validate(country);
// //
// //         if (validationResult.IsValid)
// //         {
// //             var countryDto = mapper.Map(country);
// //             return Results.CreatedAtRoute("countryById", new { Id = countryService.CreateOrUpdate(countryDto) });
// //         }
// //
// //         return Results.ValidationProblem(validationResult.ToDictionary());
// //     });
//
// // app.MapGet("/countries/{id}", (int id, ICountryMapper mapper, ICountryService countryService) =>
// // {
// //     var country = countryService.Retrieve(id);
// //
// //     if (country is null)
// //         return Results.NotFound();
// //
// //     return Results.Ok(mapper.Map(country));
// // }).WithName("countryById");
//
// // app.MapGet("/countries", (ICountryMapper mapper, ICountryService countryService) =>
// // {
// //     var countries = countryService.GetAll();
// //     return Results.Ok(mapper.Map(countries));
// // });
//
// // app.MapDelete("/countries/{id}", (int id, ICountryService countryService) =>
// // {
// //     if (countryService.Delete(id))
// //         return Results.NoContent();
// //
// //     return Results.NotFound();
// // });
//
// // app.MapPut("/countries",
// //     ([FromBody] Country country, IValidator<Country> validator, ICountryMapper mapper,
// //         ICountryService countryService) =>
// //     {
// //         var validationResult = validator.Validate(country);
// //
// //         if (validationResult.IsValid)
// //         {
// //             if (country.Id is null)
// //                 return Results.CreatedAtRoute("countryById",
// //                     new { Id = countryService.CreateOrUpdateAsync(mapper.Map(country)) });
// //             return Results.NoContent();
// //         }
// //
// //         return Results.ValidationProblem(validationResult.ToDictionary());
// //     });
//
// // app.MapPatch("/countries/{id}",
// //     (int id, [FromBody] CountryPatch countryPatch, IValidator<CountryPatch> validator, ICountryMapper mapper,
// //         ICountryService countryService) =>
// //     {
// //         var validationResult = validator.Validate(countryPatch);
// //
// //         if (validationResult.IsValid)
// //         {
// //             if (countryService.UpdateDescriptionAsync(id, countryPatch.Description))
// //                 return Results.NoContent();
// //
// //             return Results.NotFound();
// //         }
// //
// //         return Results.ValidationProblem(validationResult.ToDictionary());
// //     });
//
// /* Validation examples */
//
//
// /* Download example */
//
// // app.MapGet("/countries/download", (ICountryService countryService) =>
// //     {
// //         (byte[] fileContent, string mimeType, string fileName) = countryService.GetFileAsync();
// //
// //         if (fileContent is null || mimeType is null)
// //             return Results.NotFound();
// //
// //         return Results.File(fileContent, mimeType, fileName);
// //     })
// //     .Produces<Stream>(StatusCodes.Status200OK, "video/mp4")
// //     .Produces(StatusCodes.Status404NotFound)
// //     .Produces(StatusCodes.Status500InternalServerError)
// //     .Produces(StatusCodes.Status408RequestTimeout);
//
// /* Download example */
//
// /* Upload example */
//
// app.MapPost("/countries/upload", (IFormFile file, IValidator<IFormFile> validator) =>
// {
//     var validationResult = validator.Validate(file);
//     if (validationResult.IsValid) return Results.Created();
//     return Results.ValidationProblem(validationResult.ToDictionary());
// });
//
// app.MapPost("/countries/uploadmany", (IFormFileCollection files) => { return Results.Created(); });
//
// //WAIT FOR this bug to get fixed: https://github.com/dotnet/aspnetcore/issues/49526
// app.MapPost("/countries/uploadwithmetadata",
//     ([FromForm] CountryMetaData countryMetaData) => { return Results.Created(); }).DisableAntiforgery();
//
// //WAIT FOR this bug to get fixed: https://github.com/dotnet/aspnetcore/issues/49526
// app.MapPost("/countries/uploadmanywithmetadata",
//         ([FromForm] CountryMetaData countryMetaData, IFormFileCollection files) => { return Results.Created(); })
//     .DisableAntiforgery();
//
// /* Upload example */
//
// /* Streaming example */
//
// app.MapGet("/streaming", async (IStreamingService streamingService) =>
// {
//     var (stream, mimeType) = await streamingService.GetFileStream();
//     return Results.Stream(stream, mimeType, enableRangeProcessing: true);
// });
//
// /* Streaming example */
//
// /* Api versioning */
// // var versionSet = app.NewApiVersionSet()
// //     .HasApiVersion(1.0)
// //     .HasApiVersion(2.0)
// //     .Build();
//
// // app.MapGet("/version", () => "Hello version 1").WithApiVersionSet(versionSet).MapToApiVersion(1.0);
// // app.MapGet("/version", () => "Hello version 2").WithApiVersionSet(versionSet).MapToApiVersion(2.0);
// // app.MapGet("/version2only", () => "Hello version 2 only").WithApiVersionSet(versionSet).MapToApiVersion(2.0);
// // app.MapGet("/versionneutral",
// //     [SwaggerOperation(Summary = "Neutral version", Description = "This version is neutral")]
// //     () =>
// //         "Hello neutral version").WithApiVersionSet(versionSet).IsApiVersionNeutral();
// //.WithOpenApi(operation => new(operation)
// //{
// //    Summary = "This is a summary",
// //    Description = "This is a description"
// //});
//
// // app.MapGroup("/v1")
// //     .GroupVersion1()
// //     .WithTags("V1")
// //     .WithOpenApi(operation => new OpenApiOperation(operation)
// //     {
// //         Deprecated = true
// //     });
// //.ExcludeFromDescription();
//
// // app.MapGroup("/v2")
// //     .GroupVersion2()
// //     .WithTags("V2");
//
// /* Api versioning */
//
// // simulate back ground task
// // Task.Run(() =>
// // {
// //     Thread.Sleep(10000);
// //     Ready.IsReady = true;
// // });
//
// app.Run();

