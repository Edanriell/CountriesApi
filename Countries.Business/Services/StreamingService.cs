using Countries.Domain.Services;

namespace Countries.Business.Services;

public class StreamingService : IStreamingService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public StreamingService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<(Stream stream, string mimeType)> GetFileStream()
    {
        var client = _httpClientFactory.CreateClient();
        var stream = await client.GetStreamAsync("https://path-to-video.com/videos/space/earth.mp4");
        return (stream, "video/mp4");
    }
}