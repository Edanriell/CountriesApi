using Countries.MinimalApi.Endpoints;

namespace Countries.MinimalApi.RouteGroups;

public static class CountryGroup
{
    public static void AddCountryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/countries");

        group.MapPost("/", CountryEndpoints.PostCountry);
        // Other endpoints in the same group
    }
}