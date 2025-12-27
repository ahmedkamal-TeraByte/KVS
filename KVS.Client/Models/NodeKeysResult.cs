using System.Text.Json.Serialization;

namespace KVS.Client.Models;

public class NodeKeyResult
{
    [JsonPropertyName("key")]
    public required string Key { get; set; }
    [JsonPropertyName("node-id")]
    public required string NodeId { get; set; }
}