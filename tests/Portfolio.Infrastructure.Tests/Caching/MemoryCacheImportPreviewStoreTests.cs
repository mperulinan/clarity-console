using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Portfolio.Application.DTOs;
using Portfolio.Application.Models;
using Portfolio.Infrastructure.Caching;

namespace Portfolio.Infrastructure.Tests.Caching;

public class MemoryCacheImportPreviewStoreTests
{
    private readonly MemoryCache _memoryCache;
    private readonly MemoryCacheImportPreviewStore _sut;

    public MemoryCacheImportPreviewStoreTests()
    {
        var options = Options.Create(new MemoryCacheOptions());
        _memoryCache = new MemoryCache(options);
        _sut = new MemoryCacheImportPreviewStore(_memoryCache);
    }

    [Fact]
    public void Set_ValidSession_StoresSessionRetrievableByGet()
    {
        // Arrange
        var token = "preview-token-1";
        var session = new ImportPreviewSession(
            "degiro",
            new List<ImportedTransactionRowPreview>(),
            DateTimeOffset.UtcNow);

        // Act
        _sut.Set(token, session);
        var retrieved = _sut.Get(token);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("degiro", retrieved.BrokerKey);
    }

    [Fact]
    public void Get_UnknownToken_ReturnsNull()
    {
        // Act
        var result = _sut.Get("unknown-token");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Remove_ExistingToken_RemovesSessionFromStore()
    {
        // Arrange
        var token = "preview-token-remove";
        var session = new ImportPreviewSession(
            "degiro",
            new List<ImportedTransactionRowPreview>(),
            DateTimeOffset.UtcNow);
        _sut.Set(token, session);

        // Act
        _sut.Remove(token);
        var result = _sut.Get(token);

        // Assert
        Assert.Null(result);
    }
}

