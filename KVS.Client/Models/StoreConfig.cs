namespace KVS.Client.Models;

public class StoreConfig
{
    public required List<StoreServer> StoreServers { get; set; }
    public required int TimeoutSeconds { get; set; }
}