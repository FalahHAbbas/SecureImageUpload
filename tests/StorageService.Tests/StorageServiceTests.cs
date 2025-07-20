using StorageService;
using Xunit;
using System.Text;
using Microsoft.Extensions.Configuration;
using Moq;
using StorageService.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace StorageService.Tests;

public class HmacSignatureValidatorTests
{
    [Fact]
    public void Sign_ReturnsValidSignature()
    {
        // Arrange
        var settings = new HmacSettings { ApiSecret = "testsecret" };
        var validator = new HmacSignatureValidator(settings);
        var data = "testdata";

        // Act
        var signature = validator.Sign(data);

        // Assert
        Assert.NotNull(signature);
        Assert.NotEmpty(signature);
    }

    [Fact]
    public void IsValid_ReturnsTrue_ForValidSignature()
    {
        // Arrange
        var settings = new HmacSettings { ApiSecret = "testsecret" };
        var validator = new HmacSignatureValidator(settings);
        var data = "testdata";
        var signature = validator.Sign(data);

        // Act
        var isValid = validator.IsValid(data, signature);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void IsValid_ReturnsFalse_ForInvalidSignature()
    {
        // Arrange
        var settings = new HmacSettings { ApiSecret = "testsecret" };
        var validator = new HmacSignatureValidator(settings);
        var data = "testdata";
        var invalidSignature = "invalid";

        // Act
        var isValid = validator.IsValid(data, invalidSignature);

        // Assert
        Assert.False(isValid);
    }
}

public class StorageControllerTests
{
    private readonly Mock<IHmacSignatureValidator> _mockValidator;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<StorageController>> _mockLogger;
    private readonly StorageController _controller;

    public StorageControllerTests()
    {
        _mockValidator = new Mock<IHmacSignatureValidator>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<StorageController>>();

        _mockConfiguration.Setup(c => c["StoragePath"]).Returns("test_uploads");

        _controller = new StorageController(_mockValidator.Object, _mockConfiguration.Object, _mockLogger.Object);
    }

    [Fact]
    public void ImageExists_ReturnsOk_WhenImageExists()
    {
        // Arrange
        var imageId = "existing_image.jpg";
        var testUploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "test_uploads");
        Directory.CreateDirectory(testUploadsDir);
        File.WriteAllText(Path.Combine(testUploadsDir, imageId), "dummy content");

        // Act
        var result = _controller.ImageExists(imageId);

        // Assert
        Assert.IsType<OkResult>(result);

        // Cleanup
        Directory.Delete(testUploadsDir, true);
    }

    [Fact]
    public void ImageExists_ReturnsNotFound_WhenImageDoesNotExist()
    {
        // Arrange
        var imageId = "non_existing_image.jpg";

        // Act
        var result = _controller.ImageExists(imageId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}
