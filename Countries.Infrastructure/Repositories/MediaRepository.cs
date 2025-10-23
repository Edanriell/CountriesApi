namespace Countries.Infrastructure.Repositories;

public class MediaRepository /*: IMediaRepository*/
{
    private readonly IHttpClientFactory _httpClientFactory;

    public MediaRepository(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<(byte[] Content, string MimeType)> GetCountryFlagContent(string countryShortName,
        CancellationToken cancellationToken)
    {
        byte[] fileBytes;

        using var client = _httpClientFactory.CreateClient();
        fileBytes = await client.GetByteArrayAsync(
            $"https://path-to-image.com/countryflags/{countryShortName}.png", cancellationToken);

        return (fileBytes, "image/png");
    }
}