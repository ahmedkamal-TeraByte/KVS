using KVS.Client.Contracts;
using KVS.Structure;
using KVS.Structure.Models;
using NSubstitute;

namespace KVS.Client.Test;

/// <summary>
/// Unit tests for <see cref="StoreClient"/>.
/// </summary>
[TestFixture]
public class StoreClientTests
{
    private IStoreHttpClientFactory _mockFactory = null!;
    private IStoreHttpClient _mockStore = null!;

    [SetUp]
    public void Setup()
    {
        // Mock the IStoreClientFactory interface - this allows proper mocking with NSubstitute
        _mockFactory = Substitute.For<IStoreHttpClientFactory>();
        _mockStore = Substitute.For<IStoreHttpClient>();
        
        // Configure the factory to return our mocked IStore
        _mockFactory.GetStoreHttpClient(Arg.Any<string>()).Returns(_mockStore);
    }

    [TearDown]
    public void TearDown()
    {
        // Reset singleton instance for test isolation
        ResetSingleton();
    }

    #region GetInstance Tests

    [Test]
    public void GetInstance_ValidFactory_ReturnsInstance()
    {
        // Act
        var client = StoreClient.GetInstance(_mockFactory);

        // Assert
        Assert.That(client, Is.Not.Null);
        Assert.That(client, Is.InstanceOf<IStore>());
    }

    [Test]
    public void GetInstance_MultipleCalls_ReturnsSameInstance()
    {
        // Act
        var client1 = StoreClient.GetInstance(_mockFactory);
        var client2 = StoreClient.GetInstance(_mockFactory);

        // Assert
        Assert.That(client1, Is.SameAs(client2));
    }

    [Test]
    public void GetInstance_DifferentFactory_ReturnsFirstInstance()
    {
        // Arrange
        var factory1 = Substitute.For<IStoreHttpClientFactory>();
        var factory2 = Substitute.For<IStoreHttpClientFactory>();

        // Act
        var client1 = StoreClient.GetInstance(factory1);
        var client2 = StoreClient.GetInstance(factory2);

        // Assert - Singleton pattern means second factory is ignored
        Assert.That(client1, Is.SameAs(client2));
    }

    #endregion

    #region GetAsync Tests

    [Test]
    public async Task GetAsync_DelegatesToFactoryAndStoreClient()
    {
        // Arrange
        const string key = "test-key";
        var expectedValue = new StoreValue
        {
            Key = key,
            Value = "test-value",
            Version = 1
        };

        _mockStore.GetAsync(key).Returns(expectedValue);

        var client = StoreClient.GetInstance(_mockFactory);

        // Act
        var result = await client.GetAsync(key);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(expectedValue));
        _mockFactory.Received(1).GetStoreHttpClient(key);
        await _mockStore.Received(1).GetAsync(key);
    }

    [Test]
    public async Task GetAsync_NonExistentKey_ReturnsNull()
    {
        // Arrange
        const string key = "non-existent-key";

        _mockStore.GetAsync(key).Returns((StoreValue?)null);

        var client = StoreClient.GetInstance(_mockFactory);

        // Act
        var result = await client.GetAsync(key);

        // Assert
        Assert.That(result, Is.Null);
         _mockFactory.Received(1).GetStoreHttpClient(key);
        await _mockStore.Received(1).GetAsync(key);
    }

    [Test]
    public void GetAsync_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var client = StoreClient.GetInstance(_mockFactory);

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () => await client.GetAsync(null!));
        _mockFactory.DidNotReceive().GetStoreHttpClient(Arg.Any<string>());
    }

    #endregion

    #region PutAsync Tests

    [Test]
    public async Task PutAsync_DelegatesToFactoryAndStoreClient()
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

        _mockStore.PutAsync(key, value, 0).Returns(expectedValue);

        var client = StoreClient.GetInstance(_mockFactory);

        // Act
        var result = await client.PutAsync(key, value);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(expectedValue));
         _mockFactory.Received(1).GetStoreHttpClient(key);
        await _mockStore.Received(1).PutAsync(key, value, 0);
    }

    [Test]
    public async Task PutAsync_WithIfVersion_DelegatesToFactoryAndStoreClient()
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

        _mockStore.PutAsync(key, value, ifVersion).Returns(expectedValue);

        var client = StoreClient.GetInstance(_mockFactory);

        // Act
        var result = await client.PutAsync(key, value, ifVersion);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(expectedValue));
         _mockFactory.Received(1).GetStoreHttpClient(key);
        await _mockStore.Received(1).PutAsync(key, value, ifVersion);
    }

    [Test]
    public void PutAsync_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var client = StoreClient.GetInstance(_mockFactory);

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () => await client.PutAsync(null, "value"));
        _mockFactory.DidNotReceive().GetStoreHttpClient(Arg.Any<string>());
    }

    #endregion

    #region PatchAsync Tests

    [Test]
    public async Task PatchAsync_DelegatesToFactoryAndStoreClient()
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

        _mockStore.PatchAsync(key, delta, 0).Returns(expectedValue);

        var client = StoreClient.GetInstance(_mockFactory);

        // Act
        var result = await client.PatchAsync(key, delta);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(expectedValue));
         _mockFactory.Received(1).GetStoreHttpClient(key);
        await _mockStore.Received(1).PatchAsync(key, delta, 0);
    }

    [Test]
    public async Task PatchAsync_WithIfVersion_DelegatesToFactoryAndStoreClient()
    {
        // Arrange
        const string key = "test-key";
        const string delta = "delta-value";
        const int ifVersion = 3;
        var expectedValue = new StoreValue
        {
            Key = key,
            Value = "merged-value",
            Version = 4
        };

        _mockStore.PatchAsync(key, delta, ifVersion).Returns(expectedValue);

        var client = StoreClient.GetInstance(_mockFactory);

        // Act
        var result = await client.PatchAsync(key, delta, ifVersion);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(expectedValue));
         _mockFactory.Received(1).GetStoreHttpClient(key);
        await _mockStore.Received(1).PatchAsync(key, delta, ifVersion);
    }

    [Test]
    public void PatchAsync_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var client = StoreClient.GetInstance(_mockFactory);

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () => await client.PatchAsync(null!, "delta"));
        _mockFactory.DidNotReceive().GetStoreHttpClient(Arg.Any<string>());
    }

    #endregion

    #region Helper Methods

    private void ResetSingleton()
    {
        // Use reflection to reset the singleton instance
        var instanceField = typeof(StoreClient).GetField("_instance",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        instanceField?.SetValue(null, null);
    }

    #endregion
}

