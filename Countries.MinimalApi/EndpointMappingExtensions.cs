using Asp.Versioning;
using Asp.Versioning.Builder;
using Countries.Domain.Channels;
using Countries.Domain.DTOs;
using Countries.Domain.Services;
using Countries.MinimalApi.EndpointFilters;
using Countries.MinimalApi.Identity;
using Countries.MinimalApi.Mapping.Interfaces;
using Countries.MinimalApi.Models;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;

namespace Countries.MinimalApi;

public static class EndpointMappingExtensions
{
    public static WebApplication MapHealthCheckEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/ready", new HealthCheckOptions
        {
            Predicate = healthCheck => healthCheck.Tags.Contains("ready")
        });

        app.MapHealthChecks("/live", new HealthCheckOptions
        {
            Predicate = healthCheck => healthCheck.Tags.Contains("live")
        });

        return app;
    }

    public static WebApplication MapAuthenticationEndpoints(this WebApplication app)
    {
        app.MapGet("/authenticated", () => Results.Ok("Authenticated !"))
            .RequireAuthorization();
        // .ExcludeFromDescription();

        app.MapGet("/authorized", (IUserProfile userProfile) => Results.Ok("Authorized !"))
            .RequireAuthorization("SurveyCreator");

        return app;
    }

    public static WebApplication MapCountryEndpoints(this WebApplication app)
    {
        // Create API version set
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .HasApiVersion(new ApiVersion(2, 0))
            .ReportApiVersions()
            .Build();

        // Version 1.0 endpoints - Simple CRUD without pagination
        MapCountryV1Endpoints(app, versionSet);

        // Version 2.0 endpoints - Enhanced with pagination
        MapCountryV2Endpoints(app, versionSet);

        return app;
    }

    private static void MapCountryV1Endpoints(WebApplication app, ApiVersionSet versionSet)
    {
        var v1Group = app.MapGroup("/api/v1/countries")
            .WithApiVersionSet(versionSet)
            .MapToApiVersion(1, 0)
            .WithTags("Countries")
            .WithGroupName("v1");

        v1Group.MapGet("/{id:int}", async (int id, ICountryMapper mapper, ICountryService countryService) =>
            {
                var country = await countryService.RetrieveAsync(id);
                if (country is null)
                    return Results.NotFound();
                return Results.Ok(mapper.Map(country));
            })
            .WithName("GetCountryByIdV1")
            .WithSummary("Get country by ID")
            .WithDescription("Retrieves a single country by its ID")
            .Produces<Country>()
            .Produces(StatusCodes.Status404NotFound);

        v1Group.MapGet("/", async (ICountryMapper mapper, ICountryService countryService) =>
            {
                var countries = await countryService.GetAllAsync();
                return Results.Ok(mapper.Map(countries));
            })
            .WithName("GetAllCountriesV1")
            .WithSummary("Get all countries")
            .WithDescription("Retrieves all countries without pagination")
            .Produces<List<Country>>();

        v1Group.MapPost("/",
                async ([FromBody] Country country, ICountryMapper mapper, ICountryService countryService) =>
                {
                    var countryDto = mapper.Map(country);
                    var countryId = await countryService.CreateOrUpdateAsync(countryDto);
                    if (countryId <= 0)
                        return Results.StatusCode(StatusCodes.Status500InternalServerError);
                    return Results.CreatedAtRoute("GetCountryByIdV1", new { id = countryId });
                })
            .AddEndpointFilter<InputValidatorFilter<Country>>()
            .WithName("CreateCountryV1")
            .WithSummary("Create a new country")
            .Produces<Country>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);

        v1Group.MapPut("/", async ([FromBody] Country country, ICountryMapper mapper, ICountryService countryService) =>
            {
                var countryDto = mapper.Map(country);
                var countryId = await countryService.CreateOrUpdateAsync(countryDto);
                if (countryId <= 0)
                    return Results.StatusCode(StatusCodes.Status500InternalServerError);

                if (country.Id is null)
                    return Results.CreatedAtRoute("GetCountryByIdV1", new { id = countryId });
                return Results.NoContent();
            })
            .AddEndpointFilter<InputValidatorFilter<Country>>()
            .WithName("UpdateCountryV1")
            .WithSummary("Update a country")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        v1Group.MapPatch("/{id:int}",
                async (int id, [FromBody] CountryPatch countryPatch, ICountryService countryService) =>
                {
                    if (await countryService.UpdateDescriptionAsync(id, countryPatch.Description))
                        return Results.NoContent();
                    return Results.NotFound();
                })
            .AddEndpointFilter<InputValidatorFilter<CountryPatch>>()
            .WithName("PatchCountryV1")
            .WithSummary("Partially update a country")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        v1Group.MapDelete("/{id:int}", async (int id, ICountryService countryService) =>
            {
                if (await countryService.DeleteAsync(id))
                    return Results.NoContent();
                return Results.NotFound();
            })
            .WithName("DeleteCountryV1")
            .WithSummary("Delete a country")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static void MapCountryV2Endpoints(WebApplication app, ApiVersionSet versionSet)
    {
        var v2Group = app.MapGroup("/api/v2/countries")
            .WithApiVersionSet(versionSet)
            .MapToApiVersion(2, 0)
            .WithTags("Countries")
            .WithGroupName("v2");

        v2Group.MapGet("/{id:int}", async (int id, ICountryMapper mapper, ICountryService countryService) =>
            {
                var country = await countryService.RetrieveAsync(id);
                if (country is null)
                    return Results.NotFound();
                return Results.Ok(mapper.Map(country));
            })
            .WithName("GetCountryByIdV2")
            .WithSummary("Get country by ID (V2)")
            .WithDescription("Retrieves a single country by its ID")
            .Produces<Country>()
            .Produces(StatusCodes.Status404NotFound);

        v2Group.MapGet("/", async (
                [FromQuery] int? pageIndex,
                [FromQuery] int? pageSize,
                ICountryMapper mapper,
                ICountryService countryService) =>
            {
                var countries = await countryService.GetAllAsync(new PagingDto
                {
                    PageIndex = pageIndex ?? 1,
                    PageSize = pageSize ?? 10
                });
                return Results.Ok(mapper.Map(countries));
            })
            .WithName("GetAllCountriesV2")
            .WithSummary("Get all countries with pagination (V2)")
            .WithDescription("Retrieves countries with pagination support")
            .Produces<List<Country>>();

        v2Group.MapPost("/",
                async ([FromBody] Country country, ICountryMapper mapper, ICountryService countryService) =>
                {
                    var countryDto = mapper.Map(country);
                    var countryId = await countryService.CreateOrUpdateAsync(countryDto);
                    if (countryId <= 0)
                        return Results.StatusCode(StatusCodes.Status500InternalServerError);
                    return Results.CreatedAtRoute("GetCountryByIdV2", new { id = countryId });
                })
            .AddEndpointFilter<InputValidatorFilter<Country>>()
            .WithName("CreateCountryV2")
            .WithSummary("Create a new country (V2)")
            .Produces<Country>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);

        v2Group.MapPut("/", async ([FromBody] Country country, ICountryMapper mapper, ICountryService countryService) =>
            {
                var countryDto = mapper.Map(country);
                var countryId = await countryService.CreateOrUpdateAsync(countryDto);
                if (countryId <= 0)
                    return Results.StatusCode(StatusCodes.Status500InternalServerError);

                if (country.Id is null)
                    return Results.CreatedAtRoute("GetCountryByIdV2", new { id = countryId });
                return Results.NoContent();
            })
            .AddEndpointFilter<InputValidatorFilter<Country>>()
            .WithName("UpdateCountryV2")
            .WithSummary("Update a country (V2)")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        v2Group.MapPatch("/{id:int}",
                async (int id, [FromBody] CountryPatch countryPatch, ICountryService countryService) =>
                {
                    if (await countryService.UpdateDescriptionAsync(id, countryPatch.Description))
                        return Results.NoContent();
                    return Results.NotFound();
                })
            .AddEndpointFilter<InputValidatorFilter<CountryPatch>>()
            .WithName("PatchCountryV2")
            .WithSummary("Partially update a country (V2)")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        v2Group.MapDelete("/{id:int}", async (int id, ICountryService countryService) =>
            {
                if (await countryService.DeleteAsync(id))
                    return Results.NoContent();
                return Results.NotFound();
            })
            .WithName("DeleteCountryV2")
            .WithSummary("Delete a country (V2)")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    public static WebApplication MapFileEndpoints(this WebApplication app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .Build();

        var fileGroup = app.MapGroup("/api/v1/files")
            .WithApiVersionSet(versionSet)
            .MapToApiVersion(1, 0)
            .WithTags("Files")
            .WithGroupName("v1");

        fileGroup.MapPost("/upload",
                async (IFormFile file, ICountryFileIntegrationChannel channel, CancellationToken cancellationToken) =>
                {
                    if (await channel.SubmitAsync(file.OpenReadStream(), cancellationToken))
                        return Results.Accepted();
                    return Results.StatusCode(StatusCodes.Status500InternalServerError);
                })
            .DisableAntiforgery()
            .WithName("UploadFile")
            .WithSummary("Upload a country file")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces(StatusCodes.Status202Accepted)
            .Produces(StatusCodes.Status500InternalServerError);

        fileGroup.MapGet("/download", async (ICountryService countryService) =>
            {
                var (fileContent, mimeType, fileName) = await countryService.GetFileAsync();

                if (fileContent is null || mimeType is null)
                    return Results.NotFound();

                return Results.File(fileContent, mimeType, fileName);
            })
            .WithName("DownloadFile")
            .WithSummary("Download a country file")
            .Produces<FileContentResult>(StatusCodes.Status200OK, "image/png")
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    public static WebApplication MapCachedCountryEndpoints(this WebApplication app)
    {
        app.MapGet("/cachedcountries",
                async ([AsParameters] Paging paging, ICountryMapper mapper, ICountryService countryService) =>
                {
                    var countries = await countryService.GetAllAsync(new PagingDto
                    {
                        PageIndex = paging.PageIndex ?? 1,
                        PageSize = paging.PageSize ?? 10
                    });
                    return Results.Ok(mapper.Map(countries));
                })
            .CacheOutput("5minutes");

        app.MapGet("/cachedinmemorycountries", async (ICountryMapper mapper, ICountryService countryService) =>
        {
            var countries = await countryService.GetAllAsync(new PagingDto
            {
                PageIndex = 1,
                PageSize = 10
            });
            return Results.Ok(mapper.Map(countries));
        });

        return app;
    }

    public static WebApplication MapStreamingEndpoints(this WebApplication app)
    {
        app.MapGet("/streaming", async (IStreamingService streamingService) =>
        {
            var (stream, mimeType) = await streamingService.GetFileStream();
            return Results.Stream(stream, mimeType, enableRangeProcessing: true);
        });

        return app;
    }

    public static WebApplication MapUtilityEndpoints(this WebApplication app)
    {
        app.MapGet("/logging", (ILogger<Program> logger) =>
        {
            logger.LogInformation("/logging endpoint has been invoked.");
            return Results.Ok();
        });

        app.MapGet("/cancellable", async (ICountryService countryService, CancellationToken cancellationToken) =>
        {
            await countryService.LongRunningQueryAsync(cancellationToken);
            return Results.Ok();
        });

        app.MapGet("/longrunning", async () =>
            {
                await Task.Delay(5000);
                return Results.Ok();
            })
            .AddEndpointFilter<LogPerformanceFilter>();

        return app;
    }

    public static WebApplication MapRateLimitingEndpoints(this WebApplication app)
    {
        app.MapGet("/notlimited", () => Results.Ok())
            .DisableRateLimiting();

        app.MapGet("/limited", () => Results.Ok())
            .RequireRateLimiting("ShortLimit");

        return app;
    }

    public static WebApplication MapExceptionEndpoints(this WebApplication app)
    {
        app.MapGet("/exception", () => { throw new Exception(); });
        app.MapGet("/timeout", () => { throw new TimeoutException(); });

        return app;
    }

    public static WebApplication MapRoutingExamples(this WebApplication app)
    {
        app.MapGet("/date/{date}", (DateTime date) => date.ToString());
        app.MapGet("/uniqueidentifier/{id}", (Guid id) => id.ToString());
        app.MapGet("/provinces/{provinceId:int:max(12)}", (int provinceId) => $"ProvinceId {provinceId}");

        app.MapMethods("/users/{userId}", new List<string> { "PUT", "PATCH" }, (HttpRequest request) =>
        {
            var id = request.RouteValues["id"];
            var lastActivityDate = request.Form["lastactivitydate"];
        });

        app.MapMethods("/routeName", new List<string> { "OPTIONS", "HEAD", "TRACE" }, () =>
        {
            // Do action
        });

        return app;
    }

    public static WebApplication MapParameterBindingExamples(this WebApplication app)
    {
        app.MapPost("/Addresses", ([FromBody] Address address) => Results.Created());
        app.MapPut("/Addresses/{addressId}",
                ([FromRoute] int addressId, [FromForm] Address address) => Results.NoContent())
            .DisableAntiforgery();
        app.MapGet("/Addresses", ([FromHeader] string coordinates, [FromQuery] int? limitCountSearch) => Results.Ok());
        app.MapGet("/IdList", ([FromQuery] int[] id) => Results.Ok());
        app.MapGet("/languages", ([FromHeader(Name = "lng")] string[] lng) => Results.Ok(lng));
        app.MapGet("/countries/ids", ([FromHeader] CountryIds ids) => Results.NoContent());

        return app;
    }
}