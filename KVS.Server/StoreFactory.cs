using KVS.Server.Contracts;
using KVS.Structure;
using KVS.Structure.Models;

namespace KVS.Server;

/// <summary>
/// Factory for creating instances of <see cref="IStore"/> based on <see cref="StoreType"/>.
/// </summary>
public class StoreFactory : IStoreFactory
{
    /// <summary>
    /// Creates and returns an instance of <see cref="IStore"/> based on the specified store type.
    /// </summary>
    /// <param name="type">The type of store to create.</param>
    /// <returns>An instance of <see cref="IStore"/> corresponding to the specified type.</returns>
    /// <exception cref="NotImplementedException">
    /// Thrown when the specified store type is not yet implemented (e.g., <see cref="StoreType.File"/>).
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when an unknown or invalid <paramref name="type"/> is specified.
    /// </exception>
    public IStore GetStoreInstance(StoreType type)
    {
        return type switch
        {
            StoreType.InMemory => InMemoryStore.GetInstance(),
            StoreType.File => throw new NotImplementedException($"Store type '{StoreType.File}' is not yet implemented."),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, $"Unknown store type: {type}")
        };
    }
}