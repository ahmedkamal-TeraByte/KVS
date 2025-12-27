namespace KVS.Client.Models;

public class StoreServer
{
    public required string Url { get; set; }
    public required int Port { get; set; }
    public string? NodeId  { get; set; }
}