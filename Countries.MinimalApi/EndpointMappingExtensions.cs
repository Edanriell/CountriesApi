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

namespace Countries.MinimalApi.Extensions;

public static class EndpointMappingExtensions
{
    public static WebApplication MapHealthCheckEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/ready", new HealthCheckOptions
            {
                Predicate = healthCheck => healthCheck.Tags.Contains("ready")
            })
            .WithTags("Health")
            .WithGroupName("v1")
            .WithSummary("Readiness health check")
            .WithDescription("Checks if the application is ready to accept requests");

        app.MapHealthChecks("/live", new HealthCheckOptions
            {
                Predicate = healthCheck => healthCheck.Tags.Contains("live")
            })
            .WithTags("Health")
            .WithGroupName("v1")
            .WithSummary("Liveness health check")
            .WithDescription("Checks if the application is alive and running");

        return app;
    }

    public static WebApplication MapAuthenticationEndpoints(this WebApplication app)
    {
        app.MapGet("/authenticated", () => Results.Ok("Authenticated !"))
            .RequireAuthorization()
            .WithTags("Authentication")
            .WithGroupName("v1")
            .WithSummary("Test authentication")
            .WithDescription("Requires valid JWT token")
            .Produces<string>()
            .Produces(StatusCodes.Status401Unauthorized);

        app.MapGet("/authorized", (IUserProfile userProfile) => Results.Ok("Authorized !"))
            .RequireAuthorization("SurveyCreator")
            .WithTags("Authentication")
            .WithGroupName("v1")
            .WithSummary("Test authorization")
            .WithDescription("Requires 'SurveyCreator' role")
            .Produces<string>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

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
            .CacheOutput("5minutes")
            .WithTags("Cache")
            .WithGroupName("v1")
            .WithSummary("Get output cached countries")
            .WithDescription("Countries cached for 5 minutes using output cache")
            .Produces<List<Country>>();

        app.MapGet("/cachedinmemorycountries", async (ICountryMapper mapper, ICountryService countryService) =>
            {
                var countries = await countryService.GetAllAsync(new PagingDto
                {
                    PageIndex = 1,
                    PageSize = 10
                });
                return Results.Ok(mapper.Map(countries));
            })
            .WithTags("Cache")
            .WithGroupName("v1")
            .WithSummary("Get in-memory cached countries")
            .WithDescription("Countries cached in memory")
            .Produces<List<Country>>();

        return app;
    }

    public static WebApplication MapStreamingEndpoints(this WebApplication app)
    {
        app.MapGet("/streaming", async (IStreamingService streamingService) =>
            {
                var (stream, mimeType) = await streamingService.GetFileStream();
                return Results.Stream(stream, mimeType, enableRangeProcessing: true);
            })
            .WithTags("Streaming")
            .WithGroupName("v1")
            .WithSummary("Stream a file")
            .WithDescription("Stream a file with range processing support for video/audio")
            .Produces<FileStreamResult>();

        return app;
    }

    public static WebApplication MapUtilityEndpoints(this WebApplication app)
    {
        app.MapGet("/logging", (ILogger<Program> logger) =>
            {
                logger.LogInformation("/logging endpoint has been invoked.");
                return Results.Ok("Logging test successful");
            })
            .WithTags("Utilities")
            .WithGroupName("v1")
            .WithSummary("Test logging")
            .WithDescription("Tests Serilog logging functionality")
            .Produces<string>();

        app.MapGet("/cancellable", async (ICountryService countryService, CancellationToken cancellationToken) =>
            {
                await countryService.LongRunningQueryAsync(cancellationToken);
                return Results.Ok("Query completed");
            })
            .WithTags("Utilities")
            .WithGroupName("v1")
            .WithSummary("Test cancellation token")
            .WithDescription("Long running query (10 seconds) that can be cancelled")
            .Produces<string>();

        app.MapGet("/longrunning", async () =>
            {
                await Task.Delay(5000);
                return Results.Ok("Task completed");
            })
            .AddEndpointFilter<LogPerformanceFilter>()
            .WithTags("Utilities")
            .WithGroupName("v1")
            .WithSummary("Test performance logging")
            .WithDescription("5 second delay to test performance filter")
            .Produces<string>();

        return app;
    }

    public static WebApplication MapRateLimitingEndpoints(this WebApplication app)
    {
        app.MapGet("/notlimited", () => Results.Ok("Not rate limited"))
            .DisableRateLimiting()
            .WithTags("Rate Limiting")
            .WithGroupName("v1")
            .WithSummary("Endpoint without rate limiting")
            .WithDescription("This endpoint bypasses rate limiting")
            .Produces<string>();

        app.MapGet("/limited", () => Results.Ok("Rate limited"))
            .RequireRateLimiting("ShortLimit")
            .WithTags("Rate Limiting")
            .WithGroupName("v1")
            .WithSummary("Endpoint with rate limiting")
            .WithDescription("Limited to 2 requests per 15 seconds")
            .Produces<string>()
            .Produces(StatusCodes.Status429TooManyRequests);

        return app;
    }

    public static WebApplication MapExceptionEndpoints(this WebApplication app)
    {
        app.MapGet("/exception", () => { throw new Exception("Test exception"); })
            .WithTags("Testing")
            .WithGroupName("v1")
            .WithSummary("Test exception handling")
            .WithDescription("Throws a generic exception to test exception handlers")
            .Produces(StatusCodes.Status500InternalServerError);

        app.MapGet("/timeout", () => { throw new TimeoutException("Test timeout"); })
            .WithTags("Testing")
            .WithGroupName("v1")
            .WithSummary("Test timeout exception")
            .WithDescription("Throws a timeout exception to test timeout handler")
            .Produces(StatusCodes.Status408RequestTimeout);

        return app;
    }

    public static WebApplication MapRoutingExamples(this WebApplication app)
    {
        app.MapGet("/date/{date}", (DateTime date) => Results.Ok(date.ToString()))
            .WithTags("Examples")
            .WithGroupName("v1")
            .WithSummary("Date parameter binding example")
            .Produces<string>();

        app.MapGet("/uniqueidentifier/{id}", (Guid id) => Results.Ok(id.ToString()))
            .WithTags("Examples")
            .WithGroupName("v1")
            .WithSummary("GUID parameter binding example")
            .Produces<string>();

        app.MapGet("/provinces/{provinceId:int:max(12)}", (int provinceId) => Results.Ok($"ProvinceId {provinceId}"))
            .WithTags("Examples")
            .WithGroupName("v1")
            .WithSummary("Route constraint example")
            .WithDescription("Province ID must be an integer with max value of 12")
            .Produces<string>();

        return app;
    }

    public static WebApplication MapParameterBindingExamples(this WebApplication app)
    {
        app.MapPost("/Addresses", ([FromBody] Address address) => Results.Created())
            .WithTags("Examples")
            .WithGroupName("v1")
            .WithSummary("Body parameter binding example")
            .Produces(StatusCodes.Status201Created);

        app.MapPut("/Addresses/{addressId}",
                ([FromRoute] int addressId, [FromForm] Address address) => Results.NoContent())
            .DisableAntiforgery()
            .WithTags("Examples")
            .WithGroupName("v1")
            .WithSummary("Route and form parameter binding example")
            .Produces(StatusCodes.Status204NoContent);

        app.MapGet("/Addresses", ([FromHeader] string coordinates, [FromQuery] int? limitCountSearch) => Results.Ok())
            .WithTags("Examples")
            .WithGroupName("v1")
            .WithSummary("Header and query parameter binding example")
            .Produces(StatusCodes.Status200OK);

        app.MapGet("/IdList", ([FromQuery] int[] id) => Results.Ok(id))
            .WithTags("Examples")
            .WithGroupName("v1")
            .WithSummary("Array query parameter binding example")
            .Produces<int[]>();

        app.MapGet("/languages", ([FromHeader(Name = "lng")] string[] lng) => Results.Ok(lng))
            .WithTags("Examples")
            .WithGroupName("v1")
            .WithSummary("Custom header name binding example")
            .Produces<string[]>();

        app.MapGet("/countries/ids", ([FromHeader] CountryIds ids) => Results.Ok(ids))
            .WithTags("Examples")
            .WithGroupName("v1")
            .WithSummary("Custom model binding example")
            .Produces<CountryIds>();

        return app;
    }
}