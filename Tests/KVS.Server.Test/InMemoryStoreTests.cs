using System.Text.Json;
using KVS.Structure;

namespace KVS.Server.Test;

/// <summary>
/// Unit tests for <see cref="InMemoryStore"/>.
/// </summary>
[TestFixture]
public class InMemoryStoreTests
{
    private IStore _store = null!;

    [SetUp]
    public void Setup()
    {
        _store = InMemoryStore.GetInstance();
    }

    #region Get Tests

    [Test]
    public async Task Get_NonExistentKey_ReturnsNull()
    {
        // Act
        var result = await _store.GetAsync("non-existent-key");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task Get_ExistingKey_ReturnsCorrectValue()
    {
        // Arrange
        const string key = "test-key";
        const string value = "test-value";
        await _store.PutAsync(key, value);

        // Act
        var result = await _store.GetAsync(key);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Key, Is.EqualTo(key));
            Assert.That(result.Value, Is.EqualTo(value));
            Assert.That(result.Version, Is.EqualTo(1));
        });
    }

    #endregion

    #region Put Tests

    [Test]
    public async Task Put_NewKey_CreatesValueWithVersionOne()
    {
        // Arrange
        const string key = "new-key";
        const string value = "new-value";

        // Act
        var result = await _store.PutAsync(key, value);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Key, Is.EqualTo(key));
            Assert.That(result.Value, Is.EqualTo(value));
            Assert.That(result.Version, Is.EqualTo(1));
        });

        // Verify it's stored
        var retrieved = await _store.GetAsync(key);
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Value, Is.EqualTo(value));
    }

    [Test]
    public async Task Put_ExistingKey_IncrementsVersion()
    {
        // Arrange
        const string key = "existing-key";
        const string initialValue = "initial-value";
        const string updatedValue = "updated-value";

        await _store.PutAsync(key, initialValue);
        var initialResult = await _store.GetAsync(key);

        // Act
        var result = await _store.PutAsync(key, updatedValue);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(initialResult, Is.Not.Null);
            Assert.That(result, Is.Not.Null);
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Key, Is.EqualTo(key));
            Assert.That(result.Value, Is.EqualTo(updatedValue));
            Assert.That(result.Version, Is.EqualTo(initialResult!.Version + 1));
        });
    }

    [Test]
    public async Task Put_WithIfVersionZero_UpdatesRegardlessOfCurrentVersion()
    {
        // Arrange
        const string key = "version-test-key";
        await _store.PutAsync(key, "value1");
        await _store.PutAsync(key, "value2"); // Version is now 2

        // Act
        var result = await _store.PutAsync(key, "value3", ifVersion: 0);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.EqualTo("value3"));
            Assert.That(result.Version, Is.EqualTo(3));
        });
    }

    [Test]
    public async Task Put_WithMatchingIfVersion_UpdatesSuccessfully()
    {
        // Arrange
        const string key = "matching-version-key";
        await _store.PutAsync(key, "value1");
        await _store.PutAsync(key, "value2"); // Version is now 2

        // Act
        var result = await _store.PutAsync(key, "value3", ifVersion: 2);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.EqualTo("value3"));
            Assert.That(result.Version, Is.EqualTo(3));
        });
    }

    [Test]
    public async Task Put_WithNonMatchingIfVersion_ThrowsInvalidOperationException()
    {
        // Arrange
        const string key = "non-matching-version-key";
        await _store.PutAsync(key, "value1");
        await _store.PutAsync(key, "value2"); // Version is now 2

        // Act & Assert
        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _store.PutAsync(key, "value3", ifVersion: 1));

        Assert.That(exception, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Does.Contain("Version mismatch"));
            Assert.That(exception.Message, Does.Contain("1"));
            Assert.That(exception.Message, Does.Contain("2"));
        });

        // Verify value was not updated
        var retrieved = await _store.GetAsync(key);
        Assert.That(retrieved!.Value, Is.EqualTo("value2"));
    }

    [Test]
    public void Put_NullKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _store.PutAsync(null!, "value"));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.ParamName, Is.EqualTo("key"));
    }

    [Test]
    public void Put_EmptyKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _store.PutAsync("", "value"));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.ParamName, Is.EqualTo("key"));
    }

    [Test]
    public async Task Put_WhitespaceKey_ExecutesSuccessfully()
    {
        // Arrange
        const string key = "   ";
        const string value = "value";

        // Act
        var result = await _store.PutAsync(key, value);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Key, Is.EqualTo(key));
            Assert.That(result.Value, Is.EqualTo(value));
            Assert.That(result.Version, Is.EqualTo(1));
        });
    }

    #endregion

    #region Patch Tests

    [Test]
    public async Task Patch_NewKey_CreatesValueWithDelta()
    {
        // Arrange
        const string key = "new-patch-key";
        const string delta = "delta-value";

        // Act
        var result = await _store.PatchAsync(key, delta);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Key, Is.EqualTo(key));
            Assert.That(result.Value, Is.EqualTo(delta));
            Assert.That(result.Version, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Patch_ExistingKeyWithValidJson_MergesJsonObjects()
    {
        // Arrange
        const string key = "json-merge-key";
        const string existingJson = """{"name": "John", "age": 30}""";
        const string deltaJson = """{"age": 31, "city": "New York"}""";

        await _store.PutAsync(key, existingJson);

        // Act
        var result = await _store.PatchAsync(key, deltaJson);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Version, Is.EqualTo(2));

        // Verify merged JSON
        var parsed = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(result.Value);
        Assert.That(parsed, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(parsed!.ContainsKey("name"), Is.True);
            Assert.That(parsed.ContainsKey("age"), Is.True);
            Assert.That(parsed.ContainsKey("city"), Is.True);
            // Assert updated values
            Assert.That(parsed["name"].GetString(), Is.EqualTo("John"));
            Assert.That(parsed["age"].GetInt32(), Is.EqualTo(31)); // Updated from 30 to 31
            Assert.That(parsed["city"].GetString(), Is.EqualTo("New York"));
        });
    }

    [Test]
    public async Task Patch_ExistingKeyWithValidJson_WithCorrectVersion_MergesJsonObjects()
    {
        // Arrange
        const string key = "json-merge-version-key";
        const string existingJson = """{"name": "John", "age": 30}""";
        const string deltaJson = """{"age": 31, "city": "New York"}""";

        await _store.PutAsync(key, existingJson);
        await _store.PutAsync(key, existingJson); // Version is now 2

        // Act
        var result = await _store.PatchAsync(key, deltaJson, ifVersion: 2);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Version, Is.EqualTo(3));

        // Verify merged JSON
        var parsed = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(result.Value);
        Assert.That(parsed, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(parsed!.ContainsKey("name"), Is.True);
            Assert.That(parsed.ContainsKey("age"), Is.True);
            Assert.That(parsed.ContainsKey("city"), Is.True);
            Assert.That(parsed["name"].GetString(), Is.EqualTo("John"));
            Assert.That(parsed["age"].GetInt32(), Is.EqualTo(31));
            Assert.That(parsed["city"].GetString(), Is.EqualTo("New York"));
        });
    }

    [Test]
    public async Task Patch_ExistingKeyWithValidJson_WithInCorrectVersion_ThrowsException()
    {
        // Arrange
        const string key = "json-merge-version-error-key";
        const string existingJson = """{"name": "John", "age": 30}""";
        const string deltaJson = """{"age": 31, "city": "New York"}""";

        await _store.PutAsync(key, existingJson);
        await _store.PutAsync(key, existingJson); // Version is now 2

        // Act & Assert
        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _store.PatchAsync(key, deltaJson, ifVersion: 1));

        Assert.That(exception, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Does.Contain("Version mismatch"));
            Assert.That(exception.Message, Does.Contain("1"));
            Assert.That(exception.Message, Does.Contain("2"));
        });

        // Verify value was not updated
        var retrieved = await _store.GetAsync(key);
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Value, Is.EqualTo(existingJson));
        Assert.That(retrieved.Version, Is.EqualTo(2));
    }

    [Test]
    public async Task Patch_ExistingKeyWithNonJson_ReplacesValue()
    {
        // Arrange
        const string key = "non-json-patch-key";
        const string existingValue = "existing-value";
        const string delta = "new-delta-value";

        await _store.PutAsync(key, existingValue);

        // Act
        var result = await _store.PatchAsync(key, delta);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.EqualTo(delta));
            Assert.That(result.Version, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task Patch_ExistingKeyWithOneInvalidJson_ReplacesValue()
    {
        // Arrange
        const string key = "partial-json-key";
        const string existingValue = "not-json";
        const string deltaJson = """{"key": "value"}""";

        await _store.PutAsync(key, existingValue);

        // Act
        var result = await _store.PatchAsync(key, deltaJson);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.EqualTo(deltaJson));
            Assert.That(result.Version, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task Patch_WithMatchingIfVersion_UpdatesSuccessfully()
    {
        // Arrange
        const string key = "patch-version-key";
        await _store.PutAsync(key, "value1");
        await _store.PutAsync(key, "value2"); // Version is now 2

        // Act
        var result = await _store.PatchAsync(key, "value3", ifVersion: 2);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Version, Is.EqualTo(3));
            Assert.That(result.Value, Is.EqualTo("value3"));
        });
    }

    [Test]
    public async Task Patch_WithNonMatchingIfVersion_ThrowsInvalidOperationException()
    {
        // Arrange
        const string key = "patch-non-matching-key";
        await _store.PutAsync(key, "value1");
        await _store.PutAsync(key, "value2"); // Version is now 2

        // Act & Assert
        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _store.PatchAsync(key, "value3", ifVersion: 1));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Message, Does.Contain("Version mismatch"));
    }

    [Test]
    public void Patch_NullKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _store.PatchAsync(null!, "delta"));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.ParamName, Is.EqualTo("key"));
    }

    [Test]
    public void Patch_EmptyKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _store.PatchAsync("", "delta"));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.ParamName, Is.EqualTo("key"));
    }

    #endregion

    #region Singleton Tests

    [Test]
    public void GetInstance_ReturnsSameInstance()
    {
        // Act
        var instance1 = InMemoryStore.GetInstance();
        var instance2 = InMemoryStore.GetInstance();

        // Assert
        Assert.That(instance1, Is.SameAs(instance2));
    }

    [Test]
    public void GetInstance_ReturnsIStoreInterface()
    {
        // Act
        var instance = InMemoryStore.GetInstance();

        // Assert
        Assert.That(instance, Is.InstanceOf<IStore>());
        Assert.That(instance, Is.InstanceOf<InMemoryStore>());
    }

    #endregion

    #region Thread Safety Tests

    [Test]
    public async Task Put_ConcurrentOperationsOnSameKey_MaintainsVersionIntegrity()
    {
        // Arrange
        const string key = "concurrent-key";
        const int numberOfThreads = 10;
        const int operationsPerThread = 10;
        var exceptions = new List<Exception>();

        // Act
        var tasks = Enumerable.Range(0, numberOfThreads).Select(threadId =>
            Task.Run(async () =>
            {
                for (int i = 0; i < operationsPerThread; i++)
                {
                    try
                    {
                        await _store.PutAsync(key, $"value-{threadId}-{i}");
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }
            })).ToArray();

        await Task.WhenAll(tasks);

        // Assert
        Assert.That(exceptions, Is.Empty, "No exceptions should occur during concurrent operations");

        var finalValue = await _store.GetAsync(key);
        Assert.That(finalValue, Is.Not.Null);
        // With 10 threads and 10 operations per thread, we should have exactly 100 updates
        Assert.That(finalValue!.Version, Is.EqualTo(100));
    }

    [Test]
    public async Task Put_ConcurrentOperationsOnDifferentKeys_AllSucceed()
    {
        // Arrange
        const int numberOfKeys = 100;
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < numberOfKeys; i++)
        {
            var key = $"concurrent-key-{i}";
            tasks.Add(Task.Run(async () =>
            {
                await _store.PutAsync(key, $"value-{key}");
                var retrieved = await _store.GetAsync(key);
                Assert.That(retrieved, Is.Not.Null);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - Verify all keys were stored
        for (int i = 0; i < numberOfKeys; i++)
        {
            var key = $"concurrent-key-{i}";
            var value = await _store.GetAsync(key);
            Assert.That(value, Is.Not.Null, $"Key {key} should exist");
            Assert.Multiple(() =>
            {
                Assert.That(value!.Value, Is.EqualTo($"value-{key}"));
                Assert.That(value.Version, Is.EqualTo(1), $"Key {key} should have version 1 since each key is independent");
            });
        }
    }

    [Test]
    public async Task Put_ConcurrentVersionCheck_OnlyOneSucceeds()
    {
        // Arrange
        const string key = "version-check-key";
        await _store.PutAsync(key, "initial-value"); // Version is now 1

        const int numberOfThreads = 10;
        var successCount = 0;
        var failureCount = 0;

        // Act
        var tasks = Enumerable.Range(0, numberOfThreads).Select(_ =>
            Task.Run(async () =>
            {
                try
                {
                    await _store.PutAsync(key, "updated-value", ifVersion: 1);
                    Interlocked.Increment(ref successCount);
                }
                catch (InvalidOperationException)
                {
                    Interlocked.Increment(ref failureCount);
                }
            })).ToArray();

        await Task.WhenAll(tasks);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(successCount, Is.EqualTo(1), "Only one thread should succeed with version check");
            Assert.That(failureCount, Is.EqualTo(numberOfThreads - 1),
                "Other threads should fail with version mismatch");
        });

        var finalValue = await _store.GetAsync(key);
        Assert.That(finalValue, Is.Not.Null);
        Assert.That(finalValue!.Version, Is.EqualTo(2));
    }

    #endregion

    #region Integration Tests

    [Test]
    public async Task Put_Get_Patch_Workflow_CompletesSuccessfully()
    {
        // Arrange
        const string key = "workflow-key";
        const string initialValue = "initial";
        const string updatedValue = "updated";
        const string patchDelta = """{"extra": "data"}""";

        // Act - Put
        var putResult = await _store.PutAsync(key, initialValue);
        Assert.That(putResult.Version, Is.EqualTo(1));

        // Act - Get
        var getResult = await _store.GetAsync(key);
        Assert.That(getResult, Is.Not.Null);
        Assert.That(getResult!.Value, Is.EqualTo(initialValue));

        // Act - Put with version check
        var updateResult = await _store.PutAsync(key, updatedValue, ifVersion: 1);
        Assert.Multiple(() =>
        {
            Assert.That(updateResult.Version, Is.EqualTo(2));
            Assert.That(updateResult.Value, Is.EqualTo(updatedValue));
        });

        // Act - Put with valid JSON using version check
        const string initialJson = """{"name": "Test", "value": "initial"}""";
        var jsonPutResult = await _store.PutAsync(key, initialJson, ifVersion: 2);
        Assert.Multiple(() =>
        {
            Assert.That(jsonPutResult.Version, Is.EqualTo(3));
            Assert.That(jsonPutResult.Value, Is.EqualTo(initialJson));
        });

        // Act - Patch with JSON delta to check JSON merge
        var patchResult = await _store.PatchAsync(key, patchDelta, ifVersion: 3);
        Assert.Multiple(() =>
        {
            Assert.That(patchResult.Version, Is.EqualTo(4));
            var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(patchResult.Value);
            Assert.That(parsed, Is.Not.Null);
            Assert.That(parsed!.ContainsKey("name"), Is.True);
            Assert.That(parsed.ContainsKey("value"), Is.True);
            Assert.That(parsed.ContainsKey("extra"), Is.True);
            Assert.That(parsed["name"].GetString(), Is.EqualTo("Test"));
            Assert.That(parsed["value"].GetString(), Is.EqualTo("initial"));
            Assert.That(parsed["extra"].GetString(), Is.EqualTo("data"));
        });

        // Act - Patch with another JSON delta to continue merging
        const string jsonDelta = """{"value": "updated", "city": "New York"}""";
        var jsonPatchResult = await _store.PatchAsync(key, jsonDelta, ifVersion: 4);
        
        Assert.Multiple(() =>
        {
            Assert.That(jsonPatchResult.Version, Is.EqualTo(5));
            var parsed2 = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonPatchResult.Value);
            Assert.That(parsed2, Is.Not.Null);
            Assert.That(parsed2!.ContainsKey("name"), Is.True);
            Assert.That(parsed2.ContainsKey("value"), Is.True);
            Assert.That(parsed2.ContainsKey("extra"), Is.True);
            Assert.That(parsed2.ContainsKey("city"), Is.True);
            Assert.That(parsed2["name"].GetString(), Is.EqualTo("Test"));
            Assert.That(parsed2["value"].GetString(), Is.EqualTo("updated"));
            Assert.That(parsed2["extra"].GetString(), Is.EqualTo("data"));
            Assert.That(parsed2["city"].GetString(), Is.EqualTo("New York"));
        });

        // Assert - Final state
        var finalResult = await _store.GetAsync(key);
        Assert.That(finalResult, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(finalResult!.Version, Is.EqualTo(5));
            var finalParsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(finalResult.Value);
            Assert.That(finalParsed, Is.Not.Null);
            Assert.That(finalParsed!.ContainsKey("name"), Is.True);
            Assert.That(finalParsed.ContainsKey("value"), Is.True);
            Assert.That(finalParsed.ContainsKey("extra"), Is.True);
            Assert.That(finalParsed.ContainsKey("city"), Is.True);
            Assert.That(finalParsed["name"].GetString(), Is.EqualTo("Test"));
            Assert.That(finalParsed["value"].GetString(), Is.EqualTo("updated"));
            Assert.That(finalParsed["extra"].GetString(), Is.EqualTo("data"));
            Assert.That(finalParsed["city"].GetString(), Is.EqualTo("New York"));
        });
    }

    #endregion
}