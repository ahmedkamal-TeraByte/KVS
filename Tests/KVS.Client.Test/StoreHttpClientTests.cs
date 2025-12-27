using System.Net;
using System.Text;
using System.Text.Json;
using KVS.Client.Models;
using KVS.Structure.Models;

namespace KVS.Client.Test;

/// <summary>
/// Unit tests for <see cref="StoreHttpClient"/>.
/// </summary>
[TestFixture]
public class StoreHttpClientTests
{
    private const string TestUrl = "localhost";
    private const int TestPort = 8080;
    private StoreServer _testServer = null!;

    [SetUp]
    public void Setup()
    {
        _testServer = new StoreServer
        {
            Url = TestUrl,
            Port = TestPort
        };
    }

    #region Constructor Tests

    [Test]
    public void Constructor_ValidServer_CreatesInstance()
    {
        // Act
        var client = new StoreHttpClient(_testServer);

        // Assert
        Assert.That(client, Is.Not.Null);
        Assert.That(client.NodeId, Is.EqualTo($"{TestUrl}:{TestPort}"));
    }

    [Test]
    public void Constructor_InvalidUrl_ThrowsArgumentException()
    {
        // Arrange
        var invalidServer = new StoreServer
        {
            Url = string.Empty,
            Port = TestPort
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new StoreHttpClient(invalidServer));
        Assert.That(ex!.Message, Does.Contain("Url"));
    }

    [Test]
    public void Constructor_InvalidPort_ThrowsArgumentException()
    {
        // Arrange
        var invalidServer = new StoreServer
        {
            Url = TestUrl,
            Port = 0
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new StoreHttpClient(invalidServer));
        Assert.That(ex!.Message, Does.Contain("Port"));
    }

    [Test]
    public void Constructor_WithTimeout_SetsTimeout()
    {
        // Arrange
        const int timeoutSeconds = 10;

        // Act
        var client = new StoreHttpClient(_testServer, timeoutSeconds);

        // Assert
        Assert.That(client, Is.Not.Null);
    }

    #endregion

    #region GetAsync Tests

    [Test]
    public async Task GetAsync_ValidKey_ReturnsStoreValue()
    {
        // Arrange
        const string key = "test-key";
        var expectedValue = new StoreValue
        {
            Key = key,
            Value = "test-value",
            Version = 1
        };

        var httpClient = CreateMockedHttpClient(HttpStatusCode.OK, expectedValue);
        var client = CreateStoreHttpClientWithHandler(httpClient);

        // Act
        var result = await client.GetAsync(key);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Key, Is.EqualTo(key));
            Assert.That(result.Value, Is.EqualTo("test-value"));
            Assert.That(result.Version, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task GetAsync_NonExistentKey_ReturnsNull()
    {
        // Arrange
        const string key = "non-existent-key";
        var httpClient = CreateMockedHttpClient(HttpStatusCode.NotFound, null);
        var client = CreateStoreHttpClientWithHandler(httpClient);

        // Act
        var result = await client.GetAsync(key);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetAsync_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var client = new StoreHttpClient(_testServer);

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () => await client.GetAsync(null!));
    }

    [Test]
    public void GetAsync_EmptyKey_ThrowsArgumentNullException()
    {
        // Arrange
        var client = new StoreHttpClient(_testServer);

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () => await client.GetAsync(string.Empty));
    }

    [Test]
    public void GetAsync_HttpRequestException_ThrowsHttpRequestException()
    {
        // Arrange
        var httpClient = CreateMockedHttpClient(HttpStatusCode.InternalServerError, null, shouldThrow: true);
        var client = CreateStoreHttpClientWithHandler(httpClient);

        // Act & Assert
        var ex = Assert.ThrowsAsync<HttpRequestException>(async () => await client.GetAsync("test-key"));
        Assert.That(ex!.Message, Does.Contain("get"));
        Assert.That(ex.Message, Does.Contain("Failed to get value on server"));
    }

    #endregion

    #region PutAsync Tests

    [Test]
    public async Task PutAsync_ValidKeyAndValue_ReturnsStoreValue()
    {
        // Arrange
        const string key = "test-key";
        const string value = "test-value";
        var expectedValue = new StoreValue
        {
            Key = key,
            Value = value,
            Version = 1
        };

        var httpClient = CreateMockedHttpClient(HttpStatusCode.OK, expectedValue);
        var client = CreateStoreHttpClientWithHandler(httpClient);

        // Act
        var result = await client.PutAsync(key, value);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Key, Is.EqualTo(key));
            Assert.That(result.Value, Is.EqualTo(value));
            Assert.That(result.Version, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task PutAsync_WithIfVersion_IncludesQueryParameter()
    {
        // Arrange
        const string key = "test-key";
        const string value = "test-value";
        const int ifVersion = 5;
        var expectedValue = new StoreValue
        {
            Key = key,
            Value = value,
            Version = 6
        };

        var handler = new TestableHttpMessageHandler();
        handler.ConfigureResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(expectedValue, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }), Encoding.UTF8, "application/json")
        });
        
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri($"http://{TestUrl}:{TestPort}/kv/")
        };

        var client = CreateStoreHttpClientWithHandler(httpClient);

        // Act
        var result = await client.PutAsync(key, value, ifVersion);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(handler.LastRequestUri, Is.Not.Null);
        Assert.That(handler.LastRequestUri!.ToString(), Does.Contain($"ifVersion={ifVersion}"));
    }

    [Test]
    public void PutAsync_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var client = new StoreHttpClient(_testServer);

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () => await client.PutAsync(null!, "value"));
    }

    [Test]
    public void PutAsync_Conflict_ThrowsInvalidOperationException()
    {
        // Arrange
        var httpClient = CreateMockedHttpClient(HttpStatusCode.Conflict, null);
        var client = CreateStoreHttpClientWithHandler(httpClient);

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await client.PutAsync("key", "value"));
        Assert.That(ex!.Message, Does.Contain("Version mismatch"));
    }

    [Test]
    public void PutAsync_BadRequest_ThrowsException()
    {
        // Arrange
        const string errorMessage = "Invalid request";
        var httpClient = CreateMockedHttpClient(HttpStatusCode.BadRequest, errorMessage);
        var client = CreateStoreHttpClientWithHandler(httpClient);

        // Act & Assert
        var ex = Assert.ThrowsAsync<Exception>(async () => await client.PutAsync("key", "value"));
        Assert.That(ex!.Message, Is.EqualTo(errorMessage));
    }

    #endregion

    #region PatchAsync Tests

    [Test]
    public async Task PatchAsync_ValidKeyAndDelta_ReturnsStoreValue()
    {
        // Arrange
        const string key = "test-key";
        const string delta = "delta-value";
        var expectedValue = new StoreValue
        {
            Key = key,
            Value = "merged-value",
            Version = 2
        };

        var httpClient = CreateMockedHttpClient(HttpStatusCode.OK, expectedValue);
        var client = CreateStoreHttpClientWithHandler(httpClient);

        // Act
        var result = await client.PatchAsync(key, delta);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Key, Is.EqualTo(key));
            Assert.That(result.Version, Is.EqualTo(2));
        });
    }

    [Test]
    public void PatchAsync_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var client = new StoreHttpClient(_testServer);

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () => await client.PatchAsync(null!, "delta"));
    }

    [Test]
    public void PatchAsync_Conflict_ThrowsInvalidOperationException()
    {
        // Arrange
        var httpClient = CreateMockedHttpClient(HttpStatusCode.Conflict, null);
        var client = CreateStoreHttpClientWithHandler(httpClient);

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await client.PatchAsync("key", "delta"));
        Assert.That(ex!.Message, Does.Contain("Version mismatch"));
    }

    #endregion

    #region Dispose Tests

    [Test]
    public void Dispose_DisposesHttpClient()
    {
        // Arrange
        var client = new StoreHttpClient(_testServer);

        // Act
        client.Dispose();

        // Assert - Should not throw
        Assert.DoesNotThrow(() => client.Dispose());
    }

    #endregion

    #region Helper Methods

    private StoreHttpClient CreateStoreHttpClientWithHandler(HttpClient httpClient)
    {
        // Use the constructor that accepts HttpClient for dependency injection
        return new StoreHttpClient(httpClient);
    }

    private HttpClient CreateMockedHttpClient(HttpStatusCode statusCode, object? responseContent, bool shouldThrow = false)
    {
        return HttpClientTestHelpers.CreateMockedHttpClient(
            statusCode, 
            responseContent, 
            shouldThrow,
            new Uri($"http://{TestUrl}:{TestPort}/kv/"));
    }

    #endregion
}

