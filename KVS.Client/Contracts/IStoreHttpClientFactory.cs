namespace KVS.Client.Contracts;

/// <summary>
/// Factory interface for creating and retrieving store clients.
/// </summary>
public interface IStoreHttpClientFactory
{
    /// <summary>
    /// Gets a store client for the specified key using hash-based distribution.
    /// The key's hashcode is used to deterministically select a client from the available clients.
    /// </summary>
    /// <param name="key">The key to get a client for.</param>
    /// <returns>The <see cref="IStoreHttpClient"/> client selected based on the key's hashcode.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no clients are available in the factory.</exception>
    IStoreHttpClient GetStoreHttpClient(string key);
    
    /// <summary>
    /// Gets a randomly selected store client from the available clients.
    /// Useful for operations that don't require a specific node, such as retrieving all keys.
    /// </summary>
    /// <returns>A randomly selected <see cref="IStoreHttpClient"/> from the available clients.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no clients are available in the factory.</exception>
    IStoreHttpClient GetRandomStoreHttpClient();
    
    /// <summary>
    /// Gets all store clients managed by this factory.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{IStoreHttpClient}"/> containing all available store clients.</returns>
    IEnumerable<IStoreHttpClient> GetAllStoreHttpClients();
}

