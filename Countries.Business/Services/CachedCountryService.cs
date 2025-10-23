using Countries.Domain.DTOs;
using Countries.Domain.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Countries.Business.Services;

public class CachedCountryService : ICountryService
{
    private const string CacheKeyPrefix = "countries-";
    private const string AllCountriesCacheKey = "countries-all";
    private readonly ICountryService _countryService;
    private readonly IMemoryCache _memoryCache;

    public CachedCountryService(ICountryService countryService, IMemoryCache memoryCache)
    {
        _countryService = countryService;
        _memoryCache = memoryCache;
    }

    public async Task<List<CountryDto>> GetAllAsync(PagingDto paging)
    {
        var cachedValue = await _memoryCache.GetOrCreateAsync(
            $"{CacheKeyPrefix}{paging.PageIndex}-{paging.PageSize}",
            async cacheEntry =>
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
                return await _countryService.GetAllAsync(paging);
            });

        return cachedValue;
    }

    public async Task<List<CountryDto>> GetAllAsync()
    {
        var cachedValue = await _memoryCache.GetOrCreateAsync(
            AllCountriesCacheKey,
            async cacheEntry =>
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
                return await _countryService.GetAllAsync();
            });

        return cachedValue;
    }

    public async Task<CountryDto> RetrieveAsync(int id)
    {
        var cachedValue = await _memoryCache.GetOrCreateAsync(
            $"country-{id}",
            async cacheEntry =>
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
                return await _countryService.RetrieveAsync(id);
            });

        return cachedValue;
    }

    public async Task<int> CreateOrUpdateAsync(CountryDto country)
    {
        var result = await _countryService.CreateOrUpdateAsync(country);

        // Invalidate all country-related cache entries
        InvalidateAllCountryCache();
        if (country.Id > 0) _memoryCache.Remove($"country-{country.Id}");

        return result;
    }

    public async Task<bool> UpdateDescriptionAsync(int id, string description)
    {
        var result = await _countryService.UpdateDescriptionAsync(id, description);

        // Invalidate cache entries
        if (result)
        {
            _memoryCache.Remove($"country-{id}");
            InvalidateAllCountryCache();
        }

        return result;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var result = await _countryService.DeleteAsync(id);

        // Invalidate cache entries
        if (result)
        {
            _memoryCache.Remove($"country-{id}");
            InvalidateAllCountryCache();
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

    private void InvalidateAllCountryCache()
    {
        // Remove the "all countries" cache
        _memoryCache.Remove(AllCountriesCacheKey);

        // This simple approach removes the known cache key.
        // For pagination cache keys (countries-{pageIndex}-{pageSize}), 
        // they will expire naturally after 30 seconds.
        // If you need immediate invalidation of all pagination caches,
        // you would need to track all cache keys or use a different caching strategy.
    }
}