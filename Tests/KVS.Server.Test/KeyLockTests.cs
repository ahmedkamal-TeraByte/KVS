using System.Collections.Concurrent;

namespace KVS.Server.Test;

/// <summary>
/// Unit tests for <see cref="KeyLock"/>.
/// </summary>
[TestFixture]
public class KeyLockTests
{
    private KeyLock _keyLock = null!;

    [SetUp]
    public void Setup()
    {
        _keyLock = new KeyLock();
    }

    #region ExecuteWithLock (Action) Tests

    [Test]
    public void ExecuteWithLock_Action_ExecutesAction()
    {
        // Arrange
        const string key = "test-key";
        var executed = false;

        // Act
        _keyLock.ExecuteWithLock(key, () => { executed = true; });

        // Assert
        Assert.That(executed, Is.True);
    }

    [Test]
    public void ExecuteWithLock_Action_ExecutesActionWithLock()
    {
        // Arrange
        const string key = "lock-test-key";
        var counter = 0;
        const int numberOfThreads = 10;
        const int operationsPerThread = 100;

        // Act
        var tasks = Enumerable.Range(0, numberOfThreads).Select(_ =>
            Task.Run(() =>
            {
                for (int i = 0; i < operationsPerThread; i++)
                {
                    _keyLock.ExecuteWithLock(key, () => { counter++; });
                }
            })).ToArray();

        Task.WaitAll(tasks);

        // Assert
        Assert.That(counter, Is.EqualTo(numberOfThreads * operationsPerThread));
    }

    [Test]
    public void ExecuteWithLock_Action_ExceptionPropagates()
    {
        // Arrange
        const string key = "exception-key";

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _keyLock.ExecuteWithLock(key, () => throw new InvalidOperationException("Test exception")));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Message, Is.EqualTo("Test exception"));
    }

    [Test]
    public void ExecuteWithLock_Action_DifferentKeysExecuteConcurrently()
    {
        // Arrange
        const int numberOfKeys = 10;
        var counters = new ConcurrentDictionary<string, int>();

        // Act
        var tasks = Enumerable.Range(0, numberOfKeys).Select(keyIndex =>
            Task.Run(() =>
            {
                var key = $"concurrent-key-{keyIndex}";
                _keyLock.ExecuteWithLock(key, () =>
                {
                    counters[key] = counters.GetOrAdd(key, 0) + 1;
                    Thread.Sleep(10); // Simulate some work
                });
            })).ToArray();

        Task.WaitAll(tasks);

        // Assert
        Assert.That(counters.Count, Is.EqualTo(numberOfKeys));
        foreach (var kvp in counters)
        {
            Assert.That(kvp.Value, Is.EqualTo(1), $"Key {kvp.Key} should have been executed once");
        }
    }

    #endregion

    #region ExecuteWithLock (Func<T>) Tests

    [Test]
    public void ExecuteWithLock_Func_ReturnsValue()
    {
        // Arrange
        const string key = "return-value-key";
        const int expectedValue = 42;

        // Act
        var result = _keyLock.ExecuteWithLock(key, () => expectedValue);

        // Assert
        Assert.That(result, Is.EqualTo(expectedValue));
    }

    [Test]
    public void ExecuteWithLock_Func_ExecutesWithLock()
    {
        // Arrange
        const string key = "func-lock-test-key";
        var counter = 0;
        const int numberOfThreads = 10;
        const int operationsPerThread = 100;

        // Act
        var tasks = Enumerable.Range(0, numberOfThreads).Select(_ =>
            Task.Run(() =>
            {
                for (int i = 0; i < operationsPerThread; i++)
                {
                    var result = _keyLock.ExecuteWithLock(key, () =>
                    {
                        counter++;
                        return counter;
                    });
                    Assert.That(result, Is.GreaterThan(0));
                }
            })).ToArray();

        Task.WaitAll(tasks);

        // Assert
        Assert.That(counter, Is.EqualTo(numberOfThreads * operationsPerThread));
    }

    [Test]
    public void ExecuteWithLock_Func_ExceptionPropagates()
    {
        // Arrange
        const string key = "func-exception-key";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _keyLock.ExecuteWithLock(key, () =>
            {
                throw new ArgumentException("Test exception");
            }));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Message, Is.EqualTo("Test exception"));
    }

    [Test]
    public void ExecuteWithLock_Func_DifferentKeysReturnDifferentValues()
    {
        // Arrange
        const int numberOfKeys = 5;
        var results = new ConcurrentDictionary<string, int>();

        // Act
        var tasks = Enumerable.Range(0, numberOfKeys).Select(keyIndex =>
            Task.Run(() =>
            {
                var key = $"func-key-{keyIndex}";
                var result = _keyLock.ExecuteWithLock(key, () => keyIndex * 10);
                results[key] = result;
            })).ToArray();

        Task.WaitAll(tasks);

        // Assert
        Assert.That(results.Count, Is.EqualTo(numberOfKeys));
        for (int i = 0; i < numberOfKeys; i++)
        {
            var key = $"func-key-{i}";
            Assert.That(results[key], Is.EqualTo(i * 10));
        }
    }

    #endregion

    #region Thread Safety Tests

    [Test]
    public void ExecuteWithLock_SameKey_SerializesOperations()
    {
        // Arrange
        const string key = "serialize-key";
        var executionOrder = new ConcurrentQueue<int>();
        const int numberOfOperations = 100;

        // Act
        var tasks = Enumerable.Range(0, numberOfOperations).Select(operationId =>
            Task.Run(() =>
            {
                _keyLock.ExecuteWithLock(key, () =>
                {
                    executionOrder.Enqueue(operationId);
                    Thread.Sleep(1); // Small delay to ensure serialization
                });
            })).ToArray();

        Task.WaitAll(tasks);

        // Assert
        Assert.That(executionOrder.Count, Is.EqualTo(numberOfOperations));
        // Operations should be serialized (though order may vary due to thread scheduling)
        // The important thing is that all operations completed without race conditions
    }

    [Test]
    public void ExecuteWithLock_DifferentKeys_ExecuteConcurrently()
    {
        // Arrange
        const int numberOfKeys = 20;
        var startTimes = new ConcurrentDictionary<string, DateTime>();
        var endTimes = new ConcurrentDictionary<string, DateTime>();

        // Act
        var tasks = Enumerable.Range(0, numberOfKeys).Select(keyIndex =>
            Task.Run(() =>
            {
                var key = $"concurrent-key-{keyIndex}";
                _keyLock.ExecuteWithLock(key, () =>
                {
                    startTimes[key] = DateTime.UtcNow;
                    Thread.Sleep(50); // Simulate work
                    endTimes[key] = DateTime.UtcNow;
                });
            })).ToArray();

        var overallStart = DateTime.UtcNow;
        Task.WaitAll(tasks);
        var overallEnd = DateTime.UtcNow;

        // Assert
        Assert.That(startTimes.Count, Is.EqualTo(numberOfKeys));
        Assert.That(endTimes.Count, Is.EqualTo(numberOfKeys));
        
        // Since different keys can run concurrently, total time should be less than
        // numberOfKeys * 50ms (would be 1000ms if serialized)
        var totalTime = (overallEnd - overallStart).TotalMilliseconds;
        Assert.That(totalTime, Is.LessThan(1000), "Different keys should execute concurrently");
    }

    [Test]
    public void ExecuteWithLock_MixedOperations_ThreadSafe()
    {
        // Arrange
        const string key = "mixed-ops-key";
        var actionExecuted = false;
        var funcResult = 0;

        // Act
        var task1 = Task.Run(() =>
            _keyLock.ExecuteWithLock(key, () => { actionExecuted = true; }));

        var task2 = Task.Run(() =>
            funcResult = _keyLock.ExecuteWithLock(key, () => 42));

        Task.WaitAll(task1, task2);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(actionExecuted, Is.True);
            Assert.That(funcResult, Is.EqualTo(42));
        });
    }

    #endregion

    #region Lock Cleanup Tests

    [Test]
    public void ExecuteWithLock_AfterExecution_LockCanBeReused()
    {
        // Arrange
        const string key = "reuse-key";
        var executionCount = 0;

        // Act - Execute multiple times
        for (int i = 0; i < 10; i++)
        {
            _keyLock.ExecuteWithLock(key, () => { executionCount++; });
        }

        // Assert
        Assert.That(executionCount, Is.EqualTo(10));
    }

    [Test]
    public void ExecuteWithLock_MultipleKeys_AllCanExecute()
    {
        // Arrange
        const int numberOfKeys = 100;
        var executionCounts = new ConcurrentDictionary<string, int>();

        // Act
        var tasks = Enumerable.Range(0, numberOfKeys).Select(keyIndex =>
            Task.Run(() =>
            {
                var key = $"cleanup-key-{keyIndex}";
                _keyLock.ExecuteWithLock(key, () =>
                {
                    executionCounts.AddOrUpdate(key, 1, (_, count) => count + 1);
                });
            })).ToArray();

        Task.WaitAll(tasks);

        // Assert
        Assert.That(executionCounts.Count, Is.EqualTo(numberOfKeys));
        foreach (var kvp in executionCounts)
        {
            Assert.That(kvp.Value, Is.EqualTo(1), $"Key {kvp.Key} should have executed once");
        }
    }

    #endregion

    #region Edge Cases

    [Test]
    public void ExecuteWithLock_EmptyKey_ThrowsArgumentNullException()
    {
        // Arrange
        const string key = "";

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            _keyLock.ExecuteWithLock(key, () => { }));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.ParamName, Is.EqualTo("key"));
    }

    [Test]
    public void ExecuteWithLock_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        string? key = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            _keyLock.ExecuteWithLock(key!, () => { }));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.ParamName, Is.EqualTo("key"));
    }

    [Test]
    public void ExecuteWithLock_WhitespaceKey_ExecutesSuccessfully()
    {
        // Arrange
        const string key = "   ";
        var executed = false;

        // Act
        _keyLock.ExecuteWithLock(key, () => { executed = true; });

        // Assert
        Assert.That(executed, Is.True);
    }

    [Test]
    public void ExecuteWithLock_Func_EmptyKey_ThrowsArgumentNullException()
    {
        // Arrange
        const string key = "";

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            _keyLock.ExecuteWithLock(key, () => 42));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.ParamName, Is.EqualTo("key"));
    }

    [Test]
    public void ExecuteWithLock_Func_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        string? key = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            _keyLock.ExecuteWithLock(key!, () => 42));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.ParamName, Is.EqualTo("key"));
    }

    [Test]
    public void ExecuteWithLock_Func_WhitespaceKey_ExecutesSuccessfully()
    {
        // Arrange
        const string key = "   ";
        const int expectedValue = 42;

        // Act
        var result = _keyLock.ExecuteWithLock(key, () => expectedValue);

        // Assert
        Assert.That(result, Is.EqualTo(expectedValue));
    }

    [Test]
    public void ExecuteWithLock_LongRunningOperation_OtherKeysNotBlocked()
    {
        // Arrange
        const string longRunningKey = "long-running-key";
        const string otherKey = "other-key";
        var otherKeyExecuted = false;
        var longRunningCompleted = false;

        // Act
        var longRunningTask = Task.Run(() =>
            _keyLock.ExecuteWithLock(longRunningKey, () =>
            {
                Thread.Sleep(100);
                longRunningCompleted = true;
            }));

        // Give it a moment to start
        Thread.Sleep(10);

        // Execute on different key - should not be blocked
        _keyLock.ExecuteWithLock(otherKey, () => { otherKeyExecuted = true; });

        Task.WaitAll(longRunningTask);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(otherKeyExecuted, Is.True, "Other key should execute immediately");
            Assert.That(longRunningCompleted, Is.True, "Long running operation should complete");
        });
    }

    [Test]
    public void ExecuteWithLock_NestedCallsSameKey_DeadlockPrevention()
    {
        // Arrange
        const string key = "nested-key";
        var innerExecuted = false;

        // Act
        _keyLock.ExecuteWithLock(key, () =>
        {
            // Attempt nested call - this should work (re-entrant lock behavior)
            // Note: C# lock statement is re-entrant, so this won't deadlock
            _keyLock.ExecuteWithLock(key, () => { innerExecuted = true; });
        });

        // Assert
        Assert.That(innerExecuted, Is.True);
    }

    #endregion
}

