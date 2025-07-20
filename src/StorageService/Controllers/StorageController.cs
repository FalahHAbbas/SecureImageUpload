
using Microsoft.AspNetCore.Mvc;

namespace StorageService.Controllers;

[ApiController]
[Route("[controller]")]
public class StorageController : ControllerBase
{
    private readonly IHmacSignatureValidator _validator;
    private readonly string _storagePath;
    private readonly ILogger<StorageController> _logger;

    public StorageController(IHmacSignatureValidator validator, IConfiguration configuration, ILogger<StorageController> logger)
    {
        _validator = validator;
        _storagePath = configuration["StoragePath"] ?? "uploads";
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }
        _logger = logger;
    }

    [HttpGet("exists/{imageId}")]
    public IActionResult ImageExists(string imageId)
    {
        _logger.LogInformation("Checking for existence of image {ImageId}", imageId);
        var imagePath = Path.Combine(_storagePath, imageId);
        var exists = System.IO.File.Exists(imagePath);
        _logger.LogInformation("Image {ImageId} exists: {Exists}", imageId, exists);
        return exists ? Ok() : NotFound();
    }
}

[ApiController]
[Route("[controller]")]
public class UploadController : ControllerBase
{
    private readonly IHmacSignatureValidator _validator;
    private readonly string _storagePath;
    private readonly ILogger<UploadController> _logger;

    public UploadController(IHmacSignatureValidator validator, IConfiguration configuration, ILogger<UploadController> logger)
    {
        _validator = validator;
        _storagePath = configuration["StoragePath"] ?? "uploads";
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> UploadImage([FromQuery] string metadata, [FromQuery] string signature)
    {
        _logger.LogInformation("Attempting to upload image with metadata: {Metadata}", metadata);
        if (!_validator.IsValid(metadata, signature))
        {
            _logger.LogWarning("Invalid signature for metadata: {Metadata}", metadata);
            return Unauthorized("Invalid signature.");
        }

        var imageId = Guid.NewGuid().ToString();
        var imagePath = Path.Combine(_storagePath, imageId);

        await using var stream = new FileStream(imagePath, FileMode.Create);
        await Request.Body.CopyToAsync(stream);

        _logger.LogInformation("Image uploaded successfully with ID: {ImageId}", imageId);
        return Ok(new { ImageId = imageId });
    }
}
