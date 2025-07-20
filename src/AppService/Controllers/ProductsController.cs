
using System;
using System.Net.Http;
using AppService.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AppService.Controllers;

[ApiController]
[Route("[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IHmacService _hmacService;
    private readonly HttpClient _storageClient;
    private readonly ILogger<ProductsController> _logger;
    private readonly IConfiguration _configuration;

    public ProductsController(IHmacService hmacService, IHttpClientFactory httpClientFactory, ILogger<ProductsController> logger, IConfiguration configuration)
    {
        _hmacService = hmacService;
        _storageClient = httpClientFactory.CreateClient("StorageService");
        _logger = logger;
        _configuration = configuration;
    }

    [HttpPost("request-upload-url")]
    public IActionResult RequestUploadUrl([FromBody] ImageMetadata metadata)
    {
        _logger.LogInformation("Requesting upload URL for {FileName}", metadata.FileName);
        var metadataJson = JsonSerializer.Serialize(metadata);
        var signature = _hmacService.Sign(metadataJson);
        var uploadUrl = $"{_storageClient.BaseAddress}upload?metadata={Uri.EscapeDataString(metadataJson)}&signature={Uri.EscapeDataString(signature)}";
        var externalUploadUrl = $"{_configuration["StorageServiceExternalUrl"]}upload?metadata={Uri.EscapeDataString(metadataJson)}&signature={Uri.EscapeDataString(signature)}";

        _logger.LogInformation("Upload URL generated: {UploadUrl}", externalUploadUrl);
        return Ok(new { UploadUrl = externalUploadUrl });
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        _logger.LogInformation("Creating product {ProductName} with image {ImageId}", request.ProductName, request.ImageId);
        var response = await _storageClient.GetAsync($"storage/exists/{request.ImageId}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Image ID {ImageId} not found in storage.", request.ImageId);
            return BadRequest("Invalid Image ID.");
        }

        // In a real application, you would save the product to a database here.
        var productId = Guid.NewGuid();
        _logger.LogInformation("Product {ProductName} created with ID {ProductId}", request.ProductName, productId);
        return Ok(new { ProductId = productId, request.ProductName, request.ImageId });
    }
}
