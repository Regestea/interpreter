using interpreter.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Text;

namespace interpreter.Api.Test;

public class InterpreterControllerTests
{
    private readonly ILogger<InterpreterController> _logger;
    private readonly InterpreterController _controller;

    public InterpreterControllerTests()
    {
        _logger = Substitute.For<ILogger<InterpreterController>>();
        _controller = new InterpreterController(_logger);
    }

    #region UploadFile Tests

    [Fact]
    public async Task UploadFile_WithValidFile_ReturnsOkResult()
    {
        // Arrange
        var fileName = "test.wav";
        var content = "Test file content";
        var file = CreateMockFormFile(fileName, content, "audio/wav");

        // Act
        var result = await _controller.UploadFile(file);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        
        var response = okResult.Value;
        var messageProperty = response.GetType().GetProperty("message");
        var fileNameProperty = response.GetType().GetProperty("fileName");
        var fileSizeProperty = response.GetType().GetProperty("fileSize");
        
        Assert.Equal("File uploaded successfully", messageProperty?.GetValue(response));
        Assert.Equal(fileName, fileNameProperty?.GetValue(response));
        Assert.Equal((long)content.Length, fileSizeProperty?.GetValue(response));
    }

    [Fact]
    public async Task UploadFile_WithNullFile_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.UploadFile(null!);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
        
        var response = badRequestResult.Value;
        var errorProperty = response.GetType().GetProperty("error");
        Assert.Equal("No file provided or file is empty", errorProperty?.GetValue(response));
    }

    [Fact]
    public async Task UploadFile_WithEmptyFile_ReturnsBadRequest()
    {
        // Arrange
        var file = CreateMockFormFile("empty.wav", "", "audio/wav");

        // Act
        var result = await _controller.UploadFile(file);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task UploadFile_WithLargeFile_ReturnsOkResult()
    {
        // Arrange
        var fileName = "large.wav";
        var content = new string('A', 1024 * 1024); // 1MB file
        var file = CreateMockFormFile(fileName, content, "audio/wav");

        // Act
        var result = await _controller.UploadFile(file);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task UploadFile_LogsInformation_WhenFileReceived()
    {
        // Arrange
        var fileName = "test.wav";
        var content = "Test content";
        var file = CreateMockFormFile(fileName, content, "audio/wav");

        // Act
        await _controller.UploadFile(file);

        // Assert
        _logger.ReceivedWithAnyArgs(2).LogInformation(default!, default(object[]));
    }

    [Fact]
    public async Task UploadFile_WithDifferentContentTypes_ReturnsCorrectContentType()
    {
        // Arrange
        var fileName = "audio.wav";
        var content = "fake audio content";
        var contentType = "audio/x-wav";
        var file = CreateMockFormFile(fileName, content, contentType);

        // Act
        var result = await _controller.UploadFile(file);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        var contentTypeProperty = response!.GetType().GetProperty("contentType");
        Assert.Equal(contentType, contentTypeProperty?.GetValue(response));
    }

    [Fact]
    public async Task UploadFile_WithNonWavExtension_ReturnsBadRequest()
    {
        // Arrange
        var fileName = "test.txt";
        var content = "Test content";
        var file = CreateMockFormFile(fileName, content, "text/plain");

        // Act
        var result = await _controller.UploadFile(file);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
        
        var response = badRequestResult.Value;
        var errorProperty = response.GetType().GetProperty("error");
        Assert.Equal("Only WAV audio files are allowed", errorProperty?.GetValue(response));
    }

    [Fact]
    public async Task UploadFile_WithNonWavContentType_ReturnsBadRequest()
    {
        // Arrange
        var fileName = "test.wav";
        var content = "Test content";
        var file = CreateMockFormFile(fileName, content, "audio/mp3");

        // Act
        var result = await _controller.UploadFile(file);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
        
        var response = badRequestResult.Value;
        var errorProperty = response.GetType().GetProperty("error");
        Assert.Equal("Only WAV audio files are allowed", errorProperty?.GetValue(response));
    }

    [Fact]
    public async Task UploadFile_WithWavExtensionButWrongContentType_ReturnsBadRequest()
    {
        // Arrange
        var fileName = "test.wav";
        var content = "Test content";
        var file = CreateMockFormFile(fileName, content, "image/png");

        // Act
        var result = await _controller.UploadFile(file);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task UploadFile_WithValidWavContentTypeVariants_ReturnsOkResult()
    {
        // Test audio/wav
        var file1 = CreateMockFormFile("test1.wav", "content", "audio/wav");
        var result1 = await _controller.UploadFile(file1);
        Assert.IsType<OkObjectResult>(result1);

        // Test audio/x-wav
        var file2 = CreateMockFormFile("test2.wav", "content", "audio/x-wav");
        var result2 = await _controller.UploadFile(file2);
        Assert.IsType<OkObjectResult>(result2);

        // Test audio/wave
        var file3 = CreateMockFormFile("test3.wav", "content", "audio/wave");
        var result3 = await _controller.UploadFile(file3);
        Assert.IsType<OkObjectResult>(result3);
    }

    #endregion

    #region UploadStream Tests

    [Fact]
    public async Task UploadStream_WithValidStream_ReturnsOkResult()
    {
        // Arrange
        var streamContent = "Test stream content";
        var streamBytes = Encoding.UTF8.GetBytes(streamContent);
        var memoryStream = new MemoryStream(streamBytes);
        
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = memoryStream;
        httpContext.Request.ContentLength = streamBytes.Length;
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await _controller.UploadStream();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        
        var response = okResult.Value;
        var messageProperty = response.GetType().GetProperty("message");
        var sizeProperty = response.GetType().GetProperty("size");
        
        Assert.Equal("Stream processed successfully", messageProperty?.GetValue(response));
        Assert.Equal(streamBytes.Length, sizeProperty?.GetValue(response));
    }

    [Fact]
    public async Task UploadStream_WithEmptyStream_ReturnsBadRequest()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream();
        httpContext.Request.ContentLength = 0;
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await _controller.UploadStream();

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task UploadStream_WithFormContentType_ReturnsBadRequest()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = "multipart/form-data";
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("test"));
        httpContext.Request.ContentLength = 4;
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await _controller.UploadStream();

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task UploadStream_WithLargeStream_ReturnsOkResult()
    {
        // Arrange
        var largeContent = new byte[1024 * 1024]; // 1MB
        Array.Fill(largeContent, (byte)'A');
        var memoryStream = new MemoryStream(largeContent);
        
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = memoryStream;
        httpContext.Request.ContentLength = largeContent.Length;
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await _controller.UploadStream();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        var sizeProperty = response!.GetType().GetProperty("size");
        Assert.Equal(largeContent.Length, sizeProperty?.GetValue(response));
    }

    [Fact]
    public async Task UploadStream_LogsInformation_WhenStreamReceived()
    {
        // Arrange
        var streamContent = "Test content";
        var streamBytes = Encoding.UTF8.GetBytes(streamContent);
        var memoryStream = new MemoryStream(streamBytes);
        
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = memoryStream;
        httpContext.Request.ContentLength = streamBytes.Length;
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        await _controller.UploadStream();

        // Assert
        _logger.ReceivedWithAnyArgs(1).LogInformation(default!, default(object[]));
    }

    #endregion

    #region Helper Methods

    private IFormFile CreateMockFormFile(string fileName, string content, string contentType = "text/plain")
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        
        var file = Substitute.For<IFormFile>();
        file.FileName.Returns(fileName);
        file.Length.Returns(bytes.Length);
        file.ContentType.Returns(contentType);
        file.OpenReadStream().Returns(stream);
        file.CopyToAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => stream.CopyToAsync(callInfo.Arg<Stream>(), callInfo.Arg<CancellationToken>()));

        return file;
    }

    #endregion
}