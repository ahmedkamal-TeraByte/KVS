using KVS.Client.Contracts;
using KVS.Client.Models;
using KVS.Structure.Models;

namespace KVS.Client;

public class StoreClient : IStoreClient
{
    private static StoreClient? _instance;
    private static readonly object Lock = new();
    private readonly IStoreHttpClientFactory _factory;

    private StoreClient(IStoreHttpClientFactory storeHttpClientFactory)
    {
        _factory = storeHttpClientFactory;
    }

    public static IStoreClient GetInstance(IStoreHttpClientFactory storeHttpClientFactory)
    {
        lock (Lock)
        {
            _instance ??= new StoreClient(storeHttpClientFactory);
        }

        return _instance;
    }
    
    public static IStoreClient GetInstance(StoreConfig storeConfig)
    {
        lock (Lock)
        {
            _instance ??= new StoreClient(new StoreHttpClientFactory(storeConfig));
        }

        return _instance;
    }
    
    public async Task<StoreValue?> GetAsync(string key)
    {
        ValidateKey(key);

        var client = _factory.GetStoreHttpClient(key);
        return await client.GetAsync(key);
    }

    public async Task<ICollection<string>> GetAllKeysAsync()
    {
        var client = _factory.GetRandomStoreHttpClient();
        return await client.GetAllKeysAsync();
    }

    public async Task<StoreValue> PutAsync(string key, string value, int ifVersion = 0)
    {
        ValidateKey(key);

        var client = _factory.GetStoreHttpClient(key);
        return await client.PutAsync(key, value, ifVersion);
    }

    public async Task<StoreValue> PatchAsync(string key, string delta, int ifVersion = 0)
    {
        ValidateKey(key);

        var client = _factory.GetStoreHttpClient(key);
        return await client.PatchAsync(key, delta, ifVersion);
    }

    public async Task<ICollection<NodeKeyResult>> GetAllNodeKeysAsync()
    {
        var clients = _factory.GetAllStoreHttpClients();
        var tasks = clients.Select(client => Task.Run(async () =>
        {
            var keys = await client.GetAllKeysAsync();
            return new
            {
                NodeId = client.NodeId,
                Keys = keys
            };
        })).ToArray();

        var results = await Task.WhenAll(tasks);

        var nodeKeyResults = new List<NodeKeyResult>();
        foreach (var result in results)
        {
            foreach (var key in result.Keys)
            {
                nodeKeyResults.Add(new NodeKeyResult
                {
                    Key = key,
                    NodeId = result.NodeId
                });
            }
        }

        return nodeKeyResults;
    }

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key), "Key cannot be null or empty.");
    }
}