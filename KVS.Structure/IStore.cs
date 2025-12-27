using KVS.Structure.Models;

namespace KVS.Structure;

/// <summary>
/// Represents a thread-safe key-value store with versioning support.
/// </summary>
public interface IStore
{
    /// <summary>
    /// Retrieves the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the value to retrieve. Must not be null or empty.</param>
    /// <returns>
    /// The <see cref="StoreValue"/> associated with the key, or <c>null</c> if the key does not exist.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null or empty.</exception>
    public Task<StoreValue?> GetAsync(string key);
    
    /// <summary>
    /// Retrieves all the keys present in the store.
    /// </summary>
    /// <returns>
    /// A List containing all keys in the store. Returns an empty list if the store is empty.
    /// </returns>
    public Task<ICollection<string>> GetAllKeysAsync();

    /// <summary>
    /// Stores or updates a value for the specified key with optional version checking.
    /// </summary>
    /// <param name="key">The key to store or update. Must not be null or empty.</param>
    /// <param name="value">The value to store. Can be any string value.</param>
    /// <param name="ifVersion">
    /// Optional version check. If specified (non-zero), the operation will only succeed if the current version matches this value.
    /// If set to 0 (default), the operation will proceed regardless of the current version.
    /// </param>
    /// <returns>
    /// The updated <see cref="StoreValue"/> containing the key, value, and new version number.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null or empty.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="ifVersion"/> is specified (non-zero) and does not match the current version of the key.
    /// </exception>
    /// <remarks>
    /// <para>
    /// If the key does not exist, a new entry is created with version 1.
    /// </para>
    /// <para>
    /// If the key exists, the version is incremented by 1. If <paramref name="ifVersion"/> is specified and doesn't match
    /// the current version, an <see cref="InvalidOperationException"/> is thrown and the value is not updated.
    /// </para>
    /// <para>
    /// This operation is thread-safe and atomic. Concurrent operations on the same key are serialized.
    /// </para>
    /// </remarks>
    public Task<StoreValue> PutAsync(string key, string value, int ifVersion = 0);

    /// <summary>
    /// Partially updates a value by merging a delta with the existing value, with optional version checking.
    /// </summary>
    /// <param name="key">The key to update. Must not be null or empty.</param>
    /// <param name="delta">The partial update (delta) to merge with the existing value. Can be any string value.</param>
    /// <param name="ifVersion">
    /// Optional version check. If specified (non-zero), the operation will only succeed if the current version matches this value.
    /// If set to 0 (default), the operation will proceed regardless of the current version.
    /// </param>
    /// <returns>
    /// The updated <see cref="StoreValue"/> containing the key, merged value, and new version number.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null or empty.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="ifVersion"/> is specified (non-zero) and does not match the current version of the key.
    /// </exception>
    /// <remarks>
    /// <para>
    /// If the key does not exist, a new entry is created with the delta value and version 1.
    /// </para>
    /// <para>
    /// If the key exists and both the existing value and delta are valid JSON objects, they are merged:
    /// properties from the delta overwrite or add to properties in the existing value.
    /// If either value is not a valid JSON object, the delta replaces the existing value entirely.
    /// </para>
    /// <para>
    /// If the key exists, the version is incremented by 1. If <paramref name="ifVersion"/> is specified and doesn't match
    /// the current version, an <see cref="InvalidOperationException"/> is thrown and the value is not updated.
    /// </para>
    /// <para>
    /// This operation is thread-safe and atomic. Concurrent operations on the same key are serialized.
    /// </para>
    /// </remarks>
    public Task<StoreValue> PatchAsync(string key, string delta, int ifVersion = 0);
}