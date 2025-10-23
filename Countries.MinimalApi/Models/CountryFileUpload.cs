namespace Countries.MinimalApi.Models;

public class CountryFileUpload
{
    public IFormFile File { get; set; }

    public string AuthorName { get; set; }
    public string Description { get; set; }
}