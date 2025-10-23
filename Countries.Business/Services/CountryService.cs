using System.Reflection;
using Countries.Domain.DTOs;
using Countries.Domain.Repositories;
using Countries.Domain.Services;

namespace Countries.Business.Services;

public class CountryService : ICountryService
{
    private readonly ICountryRepository _countryRepository;

    public CountryService(ICountryRepository countryRepository)
    {
        _countryRepository = countryRepository;
    }

    public async Task<List<CountryDto>> GetAllAsync(PagingDto paging)
    {
        return await _countryRepository.GetAllAsync(paging);
    }

    public async Task LongRunningQueryAsync(CancellationToken cancellationToken)
    {
        await _countryRepository.LongRunningQueryAsync(cancellationToken);
    }

    public async Task<bool> IngestFileAsync(Stream countryFileContent)
    {
        throw new NotImplementedException();
    }

    public async Task<(byte[], string, string)> GetFileAsync()
    {
        var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"bee.png");
        return (await File.ReadAllBytesAsync(path), "image/png", "beach.png");
    }

    public async Task<bool> DeleteAsync(int id)
    {
        return await _countryRepository.DeleteAsync(id) > 0;
    }

    public async Task<List<CountryDto>> GetAllAsync()
    {
        return await _countryRepository.GetAllAsync();
    }

    public async Task<CountryDto> RetrieveAsync(int id)
    {
        return await _countryRepository.RetrieveAsync(id);
    }

    public async Task<int> CreateOrUpdateAsync(CountryDto country)
    {
        if (country?.Id == 0 || country?.Id is null)
            return await _countryRepository.CreateAsync(country);

        if (await _countryRepository.UpdateAsync(country) > 0)
            return country.Id;

        return 0;
    }

    public async Task<bool> UpdateDescriptionAsync(int id, string description)
    {
        return await _countryRepository.UpdateDescriptionAsync(id, description) > 0;
    }
}