using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using KVS.Structure;
using KVS.Structure.Models;

namespace KVS.Server;

/// <summary>
/// Thread-safe in-memory key-value store implementation with versioning support.
/// </summary>
public class InMemoryStore : IStore
{
    private static readonly Lazy<InMemoryStore> Instance = new(() => new InMemoryStore());
    private readonly ConcurrentDictionary<string, StoreValue> _store;
    private readonly KeyLock _keyLock;

    private InMemoryStore()
    {
        _store = new ConcurrentDictionary<string, StoreValue>();
        _keyLock = new KeyLock();
    }

    /// <summary>
    /// Gets the singleton instance of the InMemoryStore.
    /// </summary>
    public static IStore GetInstance() => Instance.Value;

    /// <inheritdoc />
    public Task<StoreValue?> GetAsync(string key)
    {
        return Task.FromResult<StoreValue?>(_store.GetValueOrDefault(key));
    }

    /// <inheritdoc />
    public Task<ICollection<string>> GetAllKeysAsync()
    {
        var keys = _store.Keys;
        return Task.FromResult(keys);
    }

    /// <inheritdoc />
    public Task<StoreValue> PutAsync(string key, string value, int ifVersion = 0)
    {
        ValidateKey(key);

        var result = _keyLock.ExecuteWithLock(key, () =>
        {
            var existingValue = _store.GetValueOrDefault(key);

            if (existingValue == null)
            {
                return CreateNewValue(key, value);
            }

            ValidateVersion(existingValue.Version, ifVersion);
            return UpdateValue(key, value, existingValue.Version);
        });

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<StoreValue> PatchAsync(string key, string delta, int ifVersion = 0)
    {
        ValidateKey(key);

        var result = _keyLock.ExecuteWithLock(key, () =>
        {
            var existingValue = _store.GetValueOrDefault(key);

            if (existingValue == null)
            {
                return CreateNewValue(key, delta);
            }

            ValidateVersion(existingValue.Version, ifVersion);

            var mergedValue = TryMergeJsonValues(existingValue.Value, delta);
            return UpdateValue(key, mergedValue, existingValue.Version);
        });

        return Task.FromResult(result);
    }

    #region Private methods

    private StoreValue CreateNewValue(string key, string value)
    {
        var newValue = new StoreValue
        {
            Key = key,
            Value = value,
            Version = 1
        };

        _store[key] = newValue;
        return newValue;
    }

    private StoreValue UpdateValue(string key, string value, int currentVersion)
    {
        var updatedValue = new StoreValue
        {
            Key = key,
            Value = value,
            Version = currentVersion + 1
        };

        _store[key] = updatedValue;
        return updatedValue;
    }

    private static string TryMergeJsonValues(string existingValue, string delta)
    {
        if (TryParseJsonObject(existingValue, out var existingJson) &&
            TryParseJsonObject(delta, out var deltaJson))
        {
            MergeJsonObjects(existingJson, deltaJson);
            return existingJson.ToJsonString();
        }

        return delta;
    }

    private static bool TryParseJsonObject(string json, out JsonObject jsonObject)
    {
        jsonObject = null!;

        try
        {
            var node = JsonNode.Parse(json);
            if (node is JsonObject obj)
            {
                jsonObject = obj;
                return true;
            }
        }
        catch (JsonException)
        {
            // Invalid JSON, return false
        }

        return false;
    }

    private static void MergeJsonObjects(JsonObject target, JsonObject source)
    {
        foreach (var (key, value) in source)
        {
            target[key] = value?.DeepClone();
        }
    }

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentNullException(nameof(key), "Key cannot be null or empty.");
        }
    }

    private static void ValidateVersion(int currentVersion, int expectedVersion)
    {
        if (expectedVersion != 0 && currentVersion != expectedVersion)
        {
            throw new InvalidOperationException(
                $"Version mismatch. Expected version {expectedVersion}, but current version is {currentVersion}.");
        }
    }

    #endregion
}