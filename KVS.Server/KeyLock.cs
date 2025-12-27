using System.Collections.Concurrent;

namespace KVS.Server;

/// <summary>
/// Provides thread-safe per-key locking with automatic cleanup of unused locks.
/// Each key gets its own dedicated lock, and locks are removed when no longer in use.
/// </summary>
public class KeyLock
{
    private readonly ConcurrentDictionary<string, LockEntry> _keyLocks = new();

    private class LockEntry
    {
        public readonly object Lock = new();
        public int ReferenceCount;
    }

    /// <summary>
    /// Executes an action while holding a lock for the specified key.
    /// The lock is automatically acquired and released, and cleaned up when no longer needed.
    /// </summary>
    /// <param name="key">The key to lock on</param>
    /// <param name="action">The action to execute while holding the lock</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null or empty.</exception>
    public void ExecuteWithLock(string key, Action action)
    {
        ValidateKey(key);
        var lockEntry = GetOrCreateLockEntry(key);
        try
        {
            lock (lockEntry.Lock)
            {
                action();
            }
        }
        finally
        {
            ReleaseLockEntry(key, lockEntry);
        }
    }

    /// <summary>
    /// Executes a function while holding a lock for the specified key.
    /// The lock is automatically acquired and released, and cleaned up when no longer needed.
    /// </summary>
    /// <typeparam name="T">The return type of the function</typeparam>
    /// <param name="key">The key to lock on</param>
    /// <param name="func">The function to execute while holding the lock</param>
    /// <returns>The result of the function</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null or empty.</exception>
    public T ExecuteWithLock<T>(string key, Func<T> func)
    {
        ValidateKey(key);
        var lockEntry = GetOrCreateLockEntry(key);
        try
        {
            lock (lockEntry.Lock)
            {
                return func();
            }
        }
        finally
        {
            ReleaseLockEntry(key, lockEntry);
        }
    }

    private LockEntry GetOrCreateLockEntry(string key)
    {
        return _keyLocks.AddOrUpdate(
            key,
            _ => new LockEntry { ReferenceCount = 1 },
            (_, existing) =>
            {
                Interlocked.Increment(ref existing.ReferenceCount);
                return existing;
            });
    }

    private void ReleaseLockEntry(string key, LockEntry lockEntry)
    {
        var newCount = Interlocked.Decrement(ref lockEntry.ReferenceCount);
        if (newCount == 0)
        {
            // Double-check: verify count is still 0 before removing
            // This prevents removing a lock that another thread just started using
            if (lockEntry.ReferenceCount == 0 && 
                _keyLocks.TryGetValue(key, out var currentEntry) && 
                currentEntry == lockEntry)
            {
                // Only remove if no other thread has incremented the count
                // and the entry is still the one associated with this key
                _keyLocks.TryRemove(key, out _);
            }
        }
    }

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentNullException(nameof(key), "Key cannot be null or empty.");
        }
    }
}

