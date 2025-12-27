using KVS.Server.Contracts;
using KVS.Structure;
using KVS.Structure.Models;

namespace KVS.Server.Test;

/// <summary>
/// Unit tests for <see cref="StoreFactory"/>.
/// </summary>
[TestFixture]
public class StoreFactoryTests
{
    private StoreFactory _factory = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new StoreFactory();
    }

    [Test]
    public void StoreFactory_Implements_IStoreFactory()
    {
        // Assert
        Assert.That(_factory, Is.InstanceOf<IStoreFactory>());
    }

    [Test]
    public void GetStoreInstance_InMemory_ReturnsInMemoryStoreInstance()
    {
        // Act
        var store = _factory.GetStoreInstance(StoreType.InMemory);

        // Assert
        Assert.That(store, Is.Not.Null);
        Assert.That(store, Is.InstanceOf<InMemoryStore>());
        Assert.That(store, Is.InstanceOf<IStore>());
    }

    [Test]
    public void GetStoreInstance_InMemory_ReturnsSameSingletonInstance()
    {
        // Act
        var store1 = _factory.GetStoreInstance(StoreType.InMemory);
        var store2 = _factory.GetStoreInstance(StoreType.InMemory);

        // Assert
        Assert.That(store1, Is.SameAs(store2), "Should return the same singleton instance");
    }

    [Test]
    public void GetStoreInstance_File_ThrowsNotImplementedException()
    {
        // Act & Assert
        var exception = Assert.Throws<NotImplementedException>(() =>
            _factory.GetStoreInstance(StoreType.File));

        Assert.That(exception, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Does.Contain("File"));
            Assert.That(exception.Message, Does.Contain("not yet implemented"));
        });
    }

    [Test]
    public void GetStoreInstance_InvalidStoreType_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var invalidType = (StoreType)999; // An invalid enum value

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            _factory.GetStoreInstance(invalidType));
        
        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.ParamName, Is.EqualTo("type"));
            Assert.That(exception.Message, Does.Contain("Unknown store type"));
            Assert.That(exception.ActualValue, Is.EqualTo(invalidType));
        });
    }
    

    [Test]
    public void GetStoreInstance_MultipleCallsWithDifferentTypes_HandlesCorrectly()
    {
        // Act
        var inMemoryStore = _factory.GetStoreInstance(StoreType.InMemory);
        
        // Assert
        Assert.That(inMemoryStore, Is.Not.Null);
        Assert.That(inMemoryStore, Is.InstanceOf<InMemoryStore>());

        // Act & Assert - File should throw
        Assert.Throws<NotImplementedException>(() =>
            _factory.GetStoreInstance(StoreType.File));
    }
}

