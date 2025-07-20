using System;
using AppService;
using AppService.Controllers;
using AppService.Models;
using AppService.Middleware;
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq.Protected;
using Newtonsoft.Json;

namespace AppService.Tests;

public class HmacServiceTests
{
    [Fact]
    public void Sign_ReturnsValidSignature()
    {
        // Arrange
        var settings = new HmacSettings { ApiSecret = "testsecret" };
        var service = new HmacService(settings);
        var data = "testdata";

        // Act
        var signature = service.Sign(data);

        // Assert
        Assert.NotNull(signature);
        Assert.NotEmpty(signature);
    }

    [Fact]
    public void Sign_ThrowsException_WhenApiSecretIsNull()
    {
        // Arrange
        var settings = new HmacSettings { ApiSecret = null };
        var service = new HmacService(settings);
        var data = "testdata";

        // Act & Assert
        var exception = Record.Exception(() => service.Sign(data));
        Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal("ApiSecret is not configured.", exception.Message);
    }
}

public class ProductsControllerTests
{
    private readonly Mock<IHmacService> _mockHmacService;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger<ProductsController>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly ProductsController _controller;

    public ProductsControllerTests()
    {
        _mockHmacService = new Mock<IHmacService>();
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object) { BaseAddress = new Uri("http://storageservice/") };
        _mockLogger = new Mock<ILogger<ProductsController>>();
        _mockConfiguration = new Mock<IConfiguration>();

        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(_httpClient);

        _mockConfiguration.Setup(c => c["StorageServiceExternalUrl"]).Returns("http://localhost:7072/");

        _controller = new ProductsController(
            _mockHmacService.Object,
            _mockHttpClientFactory.Object,
            _mockLogger.Object,
            _mockConfiguration.Object);
    }

    [Fact]
    public void RequestUploadUrl_ReturnsUploadUrl()
    {
        // Arrange
        var metadata = new ImageMetadata { FileName = "test.jpg", FileSize = 100, ContentType = "image/jpeg" };
        _mockHmacService.Setup(s => s.Sign(It.IsAny<string>())).Returns("signed_data");

        // Act
        var result = _controller.RequestUploadUrl(metadata);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = JsonConvert.DeserializeObject<UploadUrlResponse>(JsonConvert.SerializeObject(okResult.Value))!;
        Assert.Contains("http://localhost:7072/upload", returnValue.UploadUrl!);
        Assert.Contains("signed_data", returnValue.UploadUrl!);
    }

    [Fact]
    public async Task CreateProduct_ReturnsOk_WhenImageExists()
    {
        // Arrange
        var request = new CreateProductRequest { ProductName = "Test Product", ImageId = "test_image_id" };
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        var result = await _controller.CreateProduct(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = JsonConvert.DeserializeObject<CreateProductResponse>(JsonConvert.SerializeObject(okResult.Value))!;
        
        Assert.Equal(request.ProductName, returnValue.ProductName!);
        Assert.Equal(request.ImageId, returnValue.ImageId!);
    }

    [Fact]
    public async Task CreateProduct_ReturnsBadRequest_WhenImageDoesNotExist()
    {
        // Arrange
        var request = new CreateProductRequest { ProductName = "Test Product", ImageId = "non_existent_image_id" };
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

        // Act
        var result = await _controller.CreateProduct(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid Image ID.", badRequestResult.Value);
    }
}

public class UploadUrlResponse
{
    public string? UploadUrl { get; set; }
}

public class CreateProductResponse
{
    public Guid ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? ImageId { get; set; }
}


