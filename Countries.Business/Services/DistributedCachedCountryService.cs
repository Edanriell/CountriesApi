using System.Text.Json;
using Countries.Domain.DTOs;
using Countries.Domain.Services;
using Microsoft.Extensions.Caching.Distributed;

namespace Countries.Business.Services;

public class DistributedCachedCountryService : ICountryService
{
    private const string CacheKeyPrefix = "countries-";
    private const string AllCountriesCacheKey = "countries-all";
    private readonly ICountryService _countryService;
    private readonly IDistributedCache _distributedCache;

    public DistributedCachedCountryService(ICountryService countryService, IDistributedCache distributedCache)
    {
        _countryService = countryService;
        _distributedCache = distributedCache;
    }

    public async Task<List<CountryDto>> GetAllAsync(PagingDto paging)
    {
        var key = $"{CacheKeyPrefix}{paging.PageIndex}-{paging.PageSize}";

        var cachedValue = await _distributedCache.GetStringAsync(key);
        if (cachedValue == null)
        {
            var data = await _countryService.GetAllAsync(paging);
            await _distributedCache.SetStringAsync(key, JsonSerializer.Serialize(data), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
            });
            return data;
        }

        return JsonSerializer.Deserialize<List<CountryDto>>(cachedValue);
    }

    public async Task<List<CountryDto>> GetAllAsync()
    {
        var cachedValue = await _distributedCache.GetStringAsync(AllCountriesCacheKey);
        if (cachedValue == null)
        {
            var data = await _countryService.GetAllAsync();
            await _distributedCache.SetStringAsync(AllCountriesCacheKey, JsonSerializer.Serialize(data),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
                });
            return data;
        }

        return JsonSerializer.Deserialize<List<CountryDto>>(cachedValue);
    }

    public async Task<CountryDto> RetrieveAsync(int id)
    {
        var key = $"country-{id}";
        var cachedValue = await _distributedCache.GetStringAsync(key);

        if (cachedValue == null)
        {
            var data = await _countryService.RetrieveAsync(id);
            if (data != null)
                await _distributedCache.SetStringAsync(key, JsonSerializer.Serialize(data),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
                    });
            return data;
        }

        return JsonSerializer.Deserialize<CountryDto>(cachedValue);
    }

    public async Task<int> CreateOrUpdateAsync(CountryDto country)
    {
        var result = await _countryService.CreateOrUpdateAsync(country);

        // Invalidate all country-related cache entries
        await InvalidateAllCountryCacheAsync();
        if (country.Id > 0) await _distributedCache.RemoveAsync($"country-{country.Id}");

        return result;
    }

    public async Task<bool> UpdateDescriptionAsync(int id, string description)
    {
        var result = await _countryService.UpdateDescriptionAsync(id, description);

        // Invalidate cache entries
        if (result)
        {
            await _distributedCache.RemoveAsync($"country-{id}");
            await InvalidateAllCountryCacheAsync();
        }

        return result;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var result = await _countryService.DeleteAsync(id);

        // Invalidate cache entries
        if (result)
        {
            await _distributedCache.RemoveAsync($"country-{id}");
            await InvalidateAllCountryCacheAsync();
        }

        return result;
    }

    public async Task<(byte[], string, string)> GetFileAsync()
    {
        return await _countryService.GetFileAsync();
    }

    public async Task<bool> IngestFileAsync(Stream countryFileContent)
    {
        return await _countryService.IngestFileAsync(countryFileContent);
    }

    public async Task LongRunningQueryAsync(CancellationToken cancellationToken)
    {
        await _countryService.LongRunningQueryAsync(cancellationToken);
    }

    private async Task InvalidateAllCountryCacheAsync()
    {
        // Remove the "all countries" cache
        await _distributedCache.RemoveAsync(AllCountriesCacheKey);

        // This simple approach removes the known cache key.
        // For pagination cache keys (countries-{pageIndex}-{pageSize}), 
        // they will expire naturally after 30 seconds.
        // If you need immediate invalidation of all pagination caches,
        // you would need to track all cache keys or use cache tagging/prefixes.
    }
}