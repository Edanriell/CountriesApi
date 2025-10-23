using Countries.Domain.DTOs;
using Countries.MinimalApi.Mapping.Interfaces;
using Countries.MinimalApi.Models;

namespace Countries.MinimalApi.Mapping;

public class CountryMapper : ICountryMapper
{
    public CountryDto Map(Country country)
    {
        return country != null
            ? new CountryDto
            {
                Id = country.Id.Value,
                Name = country.Name,
                Description = country.Description,
                FlagUri = country.FlagUri
            }
            : null;
    }

    public Country Map(CountryDto country)
    {
        return country != null
            ? new Country
            {
                Id = country.Id,
                Name = country.Name,
                Description = country.Description,
                FlagUri = country.FlagUri
            }
            : null;
    }

    public List<Country> Map(List<CountryDto> countries)
    {
        return countries.Select(Map).ToList();
    }
}