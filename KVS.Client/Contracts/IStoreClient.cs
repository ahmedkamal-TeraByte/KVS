using KVS.Client.Models;
using KVS.Structure;

namespace KVS.Client.Contracts;

public interface IStoreClient : IStore
{
    public Task<ICollection<NodeKeyResult>> GetAllNodeKeysAsync();
}