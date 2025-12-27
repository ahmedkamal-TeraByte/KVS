using KVS.Structure;
using KVS.Structure.Models;

namespace KVS.Server.Contracts;

/// <summary>
/// Factory interface for creating instances of <see cref="IStore"/> based on store type.
/// </summary>
public interface IStoreFactory
{
    /// <summary>
    /// Creates and returns an instance of <see cref="IStore"/> based on the specified store type.
    /// </summary>
    /// <param name="type">The type of store to create.</param>
    /// <returns>An instance of <see cref="IStore"/> corresponding to the specified type.</returns>
    /// <exception cref="NotImplementedException">
    /// Thrown when the specified store type is not yet implemented.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when an unknown or invalid store type is specified.
    /// </exception>
    public IStore GetStoreInstance(StoreType type);
}