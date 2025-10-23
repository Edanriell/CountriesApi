using Countries.Domain.DTOs;

namespace Countries.Domain.Services;

public interface ICountryService
{
    public Task<List<CountryDto>> GetAllAsync(PagingDto paging);
    public Task LongRunningQueryAsync(CancellationToken cancellationToken);
    public Task<bool> IngestFileAsync(Stream countryFileContent);
    public Task<(byte[], string, string)> GetFileAsync();
    Task<CountryDto> RetrieveAsync(int id);
    Task<List<CountryDto>> GetAllAsync();
    Task<int> CreateOrUpdateAsync(CountryDto country);
    Task<bool> UpdateDescriptionAsync(int id, string description);
    Task<bool> DeleteAsync(int id);
}