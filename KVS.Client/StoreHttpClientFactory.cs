using KVS.Client.Contracts;
using KVS.Client.Models;

namespace KVS.Client;

/// <summary>
/// Factory for creating and managing multiple <see cref="StoreHttpClient"/> instances for different servers.
/// Maintains a mapping of server keys to their corresponding client instances.
/// </summary>
public class StoreHttpClientFactory : IStoreHttpClientFactory
{
    private readonly List<IStoreHttpClient> _clients;

    /// <summary>
    /// Initializes a new instance of <see cref="StoreHttpClientFactory"/> with the specified list of servers.
    /// </summary>
    /// <param name="config">Configuration to create store http clients</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="config"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="config"/> is empty or contains invalid servers.</exception>
    /// <exception cref="InvalidOperationException">Thrown when multiple servers have the same node ID.</exception>
    public StoreHttpClientFactory(StoreConfig config)
    {
        ArgumentNullException.ThrowIfNull(config, nameof(config));
        var servers = config.StoreServers;

        if (servers is not { Count: > 0 })
            throw new ArgumentException("At least one server must be provided.", nameof(config));

        _clients = [];
        var set = new HashSet<string>();

        foreach (var server in servers)
        {
            var client = new StoreHttpClient(server, config.TimeoutSeconds);

            var key = client.NodeId;
            if (set.Add(key))
            {
                _clients.Add(client);
            }
            else
            {
                throw new InvalidOperationException($"Multiple servers with the same node id '{key}' are not allowed.");
            }
        }
    }

    /// <summary>
    /// Gets a store client for the specified key using hash-based distribution.
    /// The key's hashcode is used to deterministically select a client from the available clients.
    /// </summary>
    /// <param name="key">The key to get a client for.</param>
    /// <returns>The <see cref="IStoreHttpClient"/> client selected based on the key's hashcode.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no clients are available in the factory.</exception>
    public IStoreHttpClient GetStoreHttpClient(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key), "Key cannot be null or empty.");

        if (_clients.Count == 0)
            throw new InvalidOperationException("No clients available in the factory.");

        var hashCode = Math.Abs(key.GetHashCode(StringComparison.Ordinal));
        var index = hashCode % _clients.Count;
        return _clients[index];
    }

    /// <summary>
    /// Gets a randomly selected store client from the available clients.
    /// Useful for operations that don't require a specific node, such as retrieving all keys.
    /// </summary>
    /// <returns>A randomly selected <see cref="IStoreHttpClient"/> from the available clients.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no clients are available in the factory.</exception>
    public IStoreHttpClient GetRandomStoreHttpClient()
    {
        if (_clients.Count == 0)
            throw new InvalidOperationException("No clients available in the factory.");

        var randomIndex = Random.Shared.Next(_clients.Count);
        return _clients[randomIndex];
    }

    /// <summary>
    /// Gets all store clients managed by this factory.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{IStoreHttpClient}"/> containing all available store clients.</returns>
    public IEnumerable<IStoreHttpClient> GetAllStoreHttpClients()
    {
        return _clients;
    }
}