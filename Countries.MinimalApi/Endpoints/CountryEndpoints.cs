using Countries.Domain.DTOs;
using Countries.Domain.Services;
using Countries.MinimalApi.Mapping.Interfaces;
using Countries.MinimalApi.Models;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Countries.MinimalApi.Endpoints;

public static class CountryEndpoints
{
    public static async Task<IResult> GetCountries(int? pageIndex, int? pageSize, ICountryMapper mapper,
        ICountryService countryService)
    {
        var paging = new PagingDto
        {
            PageIndex = pageIndex.HasValue ? pageIndex.Value : 1,
            PageSize = pageSize.HasValue ? pageSize.Value : 10
        };
        var countries = await countryService.GetAllAsync(paging);

        return Results.Ok(mapper.Map(countries));
    }

    public static IResult PostCountry([FromBody] Country country, IValidator<Country> validator, ICountryMapper mapper,
        ICountryService countryService)
    {
        var validationResult = validator.Validate(country);

        if (validationResult.IsValid)
        {
            var countryDto = mapper.Map(country);
            return Results.CreatedAtRoute("countryById", new { Id = countryService.CreateOrUpdateAsync(countryDto) });
        }

        return Results.ValidationProblem(validationResult.ToDictionary());
    }
}