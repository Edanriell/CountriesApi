using Countries.Domain.DTOs;

namespace Countries.Domain.Repositories;

public interface ICountryRepository
{
    public Task<List<CountryDto>> GetAllAsync(PagingDto paging);
    public Task LongRunningQueryAsync(CancellationToken cancellationToken);
    Task<CountryDto> RetrieveAsync(int id);
    Task<List<CountryDto>> GetAllAsync();
    Task<int> CreateAsync(CountryDto country);
    Task<int> UpdateAsync(CountryDto country);
    Task<int> UpdateDescriptionAsync(int id, string description);
    Task<int> DeleteAsync(int id);
}