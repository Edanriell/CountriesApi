using Countries.Domain.DTOs;
using Countries.MinimalApi.Models;

namespace Countries.MinimalApi.Mapping.Interfaces;

public interface ICountryMapper
{
    public CountryDto Map(Country country);
    Country Map(CountryDto country);
    List<Country> Map(List<CountryDto> countries);
}