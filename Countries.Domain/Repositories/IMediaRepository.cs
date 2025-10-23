using Refit;

namespace Countries.Domain.Repositories;

public interface IMediaRepository
{
    [Get("/countryflags/{countryShortName}.png")]
    Task<byte[]> GetCountryFlagContent(string countryShortName, CancellationToken cancellationToken);
}