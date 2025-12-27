using KVS.Structure;

namespace KVS.Client.Contracts;

public interface IStoreHttpClient : IStore
{
    public string NodeId { get; }
}