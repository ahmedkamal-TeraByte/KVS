namespace KVS.Structure.Models;

public class StoreValue
{
    public required string Key { get; set; }
    public required string Value { get; set; }
    public required int Version { get; set; }
}