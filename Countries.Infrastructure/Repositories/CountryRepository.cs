using Countries.Domain.DTOs;
using Countries.Domain.Repositories;
using Countries.Infrastructure.Database;
using Countries.Infrastructure.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Countries.Infrastructure.Repositories;

public class CountryRepository : ICountryRepository
{
    private readonly DatabaseContext _databaseContext;

    public CountryRepository(DatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
    }

    public async Task<List<CountryDto>> GetAllAsync(PagingDto paging)
    {
        return await _databaseContext.Countries
            .AsNoTracking()
            .Select(x => new CountryDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                FlagUri = x.FlagUri
            })
            .Skip((paging.PageIndex - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .ToListAsync();
    }

    public async Task LongRunningQueryAsync(CancellationToken cancellationToken)
    {
        await _databaseContext.Database.ExecuteSqlRawAsync("WAITFOR DELAY '00:00:10'",
            cancellationToken);
    }

    public async Task<int> CreateAsync(CountryDto country)
    {
        var countryEntity = new CountryEntity
        {
            Name = country.Name,
            Description = country.Description,
            FlagUri = country.FlagUri
        };

        await _databaseContext.AddAsync(countryEntity);
        await _databaseContext.SaveChangesAsync();

        return countryEntity.Id;
    }

    public async Task<int> UpdateAsync(CountryDto country)
    {
        var countryEntity = new CountryEntity
        {
            Id = country.Id,
            Name = country.Name,
            Description = country.Description,
            FlagUri = country.FlagUri
        };

        return await _databaseContext.Countries
            .Where(x => x.Id == countryEntity.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Description, countryEntity.Description)
                .SetProperty(p => p.FlagUri, countryEntity.FlagUri)
                .SetProperty(p => p.Name, countryEntity.Name));
    }

    public async Task<int> DeleteAsync(int id)
    {
        return await _databaseContext.Countries
            .Where(x => x.Id == id)
            .ExecuteDeleteAsync();
    }

    public async Task<List<CountryDto>> GetAllAsync()
    {
        var result = await Task.Run(() => 1 + 1);
        return await _databaseContext.Countries
            .AsNoTracking()
            .Select(x => new CountryDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                FlagUri = x.FlagUri
            })
            .ToListAsync();
    }

    public async Task<CountryDto> RetrieveAsync(int id)
    {
        return await _databaseContext.Countries
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new CountryDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                FlagUri = x.FlagUri
            })
            .FirstOrDefaultAsync();
    }

    public async Task<int> UpdateDescriptionAsync(int id, string description)
    {
        return await _databaseContext.Countries
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Description, description));
    }
}