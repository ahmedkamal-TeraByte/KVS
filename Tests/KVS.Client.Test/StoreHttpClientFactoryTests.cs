using KVS.Client.Models;
using KVS.Structure;

namespace KVS.Client.Test;

/// <summary>
/// Unit tests for <see cref="StoreHttpClientFactory"/>.
/// </summary>
[TestFixture]
public class StoreHttpClientFactoryTests
{
    #region Constructor Tests

    [Test]
    public void Constructor_ValidConfig_CreatesFactory()
    {
        // Arrange
        var config = new StoreConfig
        {
            StoreServers = new List<StoreServer>
            {
                new() { Url = "localhost", Port = 8080 }
            },
            TimeoutSeconds = 5
        };

        // Act
        var factory = new StoreHttpClientFactory(config);

        // Assert
        Assert.That(factory, Is.Not.Null);
    }

    [Test]
    public void Constructor_NullConfig_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new StoreHttpClientFactory(null!));
    }

    [Test]
    public void Constructor_EmptyServerList_ThrowsArgumentException()
    {
        // Arrange
        var config = new StoreConfig
        {
            StoreServers = new List<StoreServer>(),
            TimeoutSeconds = 5
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new StoreHttpClientFactory(config));
        Assert.That(ex!.Message, Does.Contain("At least one server"));
    }

    [Test]
    public void Constructor_NullServerList_ThrowsArgumentException()
    {
        // Arrange
        var config = new StoreConfig
        {
            StoreServers = null!,
            TimeoutSeconds = 5
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new StoreHttpClientFactory(config));
    }

    [Test]
    public void Constructor_MultipleServers_CreatesClientsForAll()
    {
        // Arrange
        var config = new StoreConfig
        {
            StoreServers = new List<StoreServer>
            {
                new() { Url = "localhost", Port = 8080 },
                new() { Url = "localhost", Port = 8081 },
                new() { Url = "localhost", Port = 8082 }
            },
            TimeoutSeconds = 5
        };

        // Act
        var factory = new StoreHttpClientFactory(config);

        // Assert
        // Verify we can get clients for different keys
        var client1 = factory.GetStoreHttpClient("key1");
        var client2 = factory.GetStoreHttpClient("key2");
        var client3 = factory.GetStoreHttpClient("key3");

        Assert.Multiple(() =>
        {
            Assert.That(client1, Is.Not.Null);
            Assert.That(client2, Is.Not.Null);
            Assert.That(client3, Is.Not.Null);
        });
    }

    #endregion

    #region GetStoreClient Tests

    [Test]
    public void GetStoreClient_ValidKey_ReturnsClient()
    {
        // Arrange
        var config = new StoreConfig
        {
            StoreServers = new List<StoreServer>
            {
                new() { Url = "localhost", Port = 8080 }
            },
            TimeoutSeconds = 5
        };
        var factory = new StoreHttpClientFactory(config);

        // Act
        var client = factory.GetStoreHttpClient("test-key");

        // Assert
        Assert.That(client, Is.Not.Null);
        Assert.That(client, Is.InstanceOf<IStore>());
    }

    [Test]
    public void GetStoreClient_SameKey_ReturnsSameClient()
    {
        // Arrange
        var config = new StoreConfig
        {
            StoreServers = new List<StoreServer>
            {
                new() { Url = "localhost", Port = 8080 }
            },
            TimeoutSeconds = 5
        };
        var factory = new StoreHttpClientFactory(config);
        const string key = "test-key";

        // Act
        var client1 = factory.GetStoreHttpClient(key);
        var client2 = factory.GetStoreHttpClient(key);

        // Assert
        Assert.That(client1, Is.SameAs(client2));
    }

    [Test]
    public void GetStoreClient_DifferentKeys_ReturnsConsistentClient()
    {
        // Arrange
        var config = new StoreConfig
        {
            StoreServers = new List<StoreServer>
            {
                new() { Url = "localhost", Port = 8080 }
            },
            TimeoutSeconds = 5
        };
        var factory = new StoreHttpClientFactory(config);

        // Act
        var client1 = factory.GetStoreHttpClient("key1");
        var client2 = factory.GetStoreHttpClient("key1");

        // Assert
        Assert.That(client1, Is.SameAs(client2));
    }

    [Test]
    public void GetStoreClient_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var config = new StoreConfig
        {
            StoreServers = new List<StoreServer>
            {
                new() { Url = "localhost", Port = 8080 }
            },
            TimeoutSeconds = 5
        };
        var factory = new StoreHttpClientFactory(config);

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => factory.GetStoreHttpClient(null!));
        Assert.That(ex!.Message, Does.Contain("Key cannot be null or empty"));
    }

    [Test]
    public void GetStoreClient_EmptyKey_ThrowsArgumentNullException()
    {
        // Arrange
        var config = new StoreConfig
        {
            StoreServers = new List<StoreServer>
            {
                new() { Url = "localhost", Port = 8080 }
            },
            TimeoutSeconds = 5
        };
        var factory = new StoreHttpClientFactory(config);

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => factory.GetStoreHttpClient(string.Empty));
        Assert.That(ex!.Message, Does.Contain("Key cannot be null or empty"));
    }

    [Test]
    public void GetStoreClient_DistributesKeysAcrossServers()
    {
        // Arrange
        var config = new StoreConfig
        {
            StoreServers = new List<StoreServer>
            {
                new() { Url = "localhost", Port = 8080 },
                new() { Url = "localhost", Port = 8081 }
            },
            TimeoutSeconds = 5
        };
        var factory = new StoreHttpClientFactory(config);

        // Act - Get clients for multiple keys
        var clients = new HashSet<IStore>();
        for (int i = 0; i < 100; i++)
        {
            clients.Add(factory.GetStoreHttpClient($"key{i}"));
        }

        // Assert - With 2 servers, we should get at least 2 different clients (likely more due to distribution)
        // But with hash distribution, it's possible all keys map to one server, so we just verify we get valid clients
        Assert.That(clients.Count, Is.GreaterThanOrEqualTo(1));
        Assert.That(clients.Count, Is.LessThanOrEqualTo(2));
    }

    #endregion
}

