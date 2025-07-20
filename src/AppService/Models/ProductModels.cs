
namespace AppService.Models;

public class ImageMetadata
{
    public string? FileName { get; set; }
    public long FileSize { get; set; }
    public string? ContentType { get; set; }
}

public class CreateProductRequest
{
    public string? ProductName { get; set; }
    public string? ImageId { get; set; }
}
