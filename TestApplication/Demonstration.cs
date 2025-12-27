using KVS.Structure;

namespace TestApplication;

public static class Demonstration
{
     public static async Task DemonstrateSimpleOperations(IStore storeClient)
    {
        const string testKey = "test-key-1";

        var isUnexpected = false;

        // 1. Put operation - Create a new key-value pair
        Console.WriteLine("1. PUT Operation - Creating a new key-value pair");
        var putResult = await storeClient.PutAsync(testKey, "Initial Value");
        Console.WriteLine($"   Key: {putResult.Key}, Value: {putResult.Value}, Version: {putResult.Version}\n");

        // 2. Get operation - Retrieve the value
        Console.WriteLine("2. GET Operation - Retrieving the value");
        var getResult = await storeClient.GetAsync(testKey);
        if (getResult != null)
        {
            Console.WriteLine($"   Key: {getResult.Key}, Value: {getResult.Value}, Version: {getResult.Version}\n");
        }
        else
        {
            isUnexpected = true;
            Console.WriteLine("   Key not found.\n");
        }

        // 3. Put operation with version check - Update with version validation
        Console.WriteLine("3. PUT Operation with version check - Updating with version validation");
        try
        {
            var putWithVersionResult = await storeClient.PutAsync(testKey, "Updated Value", putResult.Version);
            Console.WriteLine($"   Key: {putWithVersionResult.Key}, Value: {putWithVersionResult.Value}, Version: {putWithVersionResult.Version}\n");
        }
        catch (InvalidOperationException ex)
        {
            isUnexpected = true;
            Console.WriteLine($"   Version mismatch: {ex.Message}\n");
        }

        // 4. Patch operation - Partial update
        Console.WriteLine("4. PATCH Operation - Partial update");
        var patchResult = await storeClient.PatchAsync(testKey, "Patched Value");
        Console.WriteLine($"   Key: {patchResult.Key}, Value: {patchResult.Value}, Version: {patchResult.Version}\n");

        // 5. Get operation after patch - Verify the update
        Console.WriteLine("5. GET Operation - Verifying the patch update");
        var getAfterPatchResult = await storeClient.GetAsync(testKey);
        if (getAfterPatchResult != null)
        {
            Console.WriteLine($"   Key: {getAfterPatchResult.Key}, Value: {getAfterPatchResult.Value}, Version: {getAfterPatchResult.Version}\n");
        }

        // 6. Put operation with wrong version - Should fail
        Console.WriteLine("6. PUT Operation with wrong version - Should fail");
        try
        {
            var wrongVersionResult = await storeClient.PutAsync(testKey, "Should Not Update", 999);
            isUnexpected = true;
            Console.WriteLine($"   Unexpected success: {wrongVersionResult.Value}\n");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"   Expected error: {ex.Message}\n");
        }

        // 7. Get operation for non-existent key
        Console.WriteLine("7. GET Operation - Non-existent key");
        var nonExistentResult = await storeClient.GetAsync("non-existent-key");
        if (nonExistentResult == null)
        {
            Console.WriteLine("   Key not found (as expected).\n");
        }
        else
        {
            isUnexpected = true;
        }

        Console.WriteLine(isUnexpected
            ? "[Failed] There was some unexpected responses!"
            : "[Success] All operations completed successfully!");
    }

    public static async Task DemonstrateConcurrentCounterIncrement(IStore storeClient)
    {
        const string counterKey = "concurrent-counter";
        const int numberOfClients = 3;
        const int incrementsPerClient = 100;
        const int expectedFinalValue = numberOfClients * incrementsPerClient; // 300

        Console.WriteLine($"Demonstrating {numberOfClients} concurrent clients incrementing a counter");
        Console.WriteLine($"Each client will perform {incrementsPerClient} increments");
        Console.WriteLine($"Expected final value: {expectedFinalValue}\n");

        // Initialize counter to 0
        Console.WriteLine("Initializing counter to 0...");
        var initialResult = await storeClient.PutAsync(counterKey, "0");
        Console.WriteLine($"Initial value: {initialResult.Value}, Version: {initialResult.Version}\n");

        // Helper method to perform a single increment with retry on version conflict
        async Task<int> IncrementCounterAsync(int clientId, int incrementNumber)
        {
            const int maxRetries = 100; // Prevent infinite loops
            int retries = 0;

            while (retries < maxRetries)
            {
                try
                {
                    // Get current value
                    var current = await storeClient.GetAsync(counterKey);
                    if (current == null)
                    {
                        // Key doesn't exist, create it
                        var newValue = await storeClient.PutAsync(counterKey, "1");
                        return int.Parse(newValue.Value);
                    }

                    // Parse current value and increment
                    var currentValue = int.Parse(current.Value);
                    var newValueStr = (currentValue + 1).ToString();

                    // Try to update with version check
                    var updated = await storeClient.PutAsync(counterKey, newValueStr, current.Version);
                    return int.Parse(updated.Value);
                }
                catch (InvalidOperationException)
                {
                    // Version conflict - retry
                    retries++;
                    await Task.Delay(Random.Shared.Next(1, 10)); // Small random delay to reduce contention
                }
            }

            throw new Exception($"Failed to increment after {maxRetries} retries");
        }

        // Create tasks for each client
        var clientTasks = new List<Task<List<int>>>();
        var startTime = DateTime.UtcNow;

        for (int clientId = 1; clientId <= numberOfClients; clientId++)
        {
            int capturedClientId = clientId;
            var task = Task.Run(async () =>
            {
                var increments = new List<int>();
                for (int i = 1; i <= incrementsPerClient; i++)
                {
                    var newValue = await IncrementCounterAsync(capturedClientId, i);
                    increments.Add(newValue);
                    
                    // Log progress every 25 increments
                    if (i % 25 == 0)
                    {
                        Console.WriteLine($"Client {capturedClientId}: Completed {i}/{incrementsPerClient} increments (current counter: {newValue})");
                    }
                }
                return increments;
            });
            clientTasks.Add(task);
        }

        Console.WriteLine($"\nStarting {numberOfClients} concurrent clients...\n");

        // Wait for all clients to complete
        var results = await Task.WhenAll(clientTasks);
        var endTime = DateTime.UtcNow;
        var duration = endTime - startTime;

        // Verify final value
        Console.WriteLine("\nAll clients completed. Verifying final counter value...");
        var finalResult = await storeClient.GetAsync(counterKey);
        
        if (finalResult != null)
        {
            var finalValue = int.Parse(finalResult.Value);
            Console.WriteLine($"Final counter value: {finalValue}");
            Console.WriteLine($"Expected value: {expectedFinalValue}");
            Console.WriteLine($"Total increments: {numberOfClients * incrementsPerClient}");
            Console.WriteLine($"Duration: {duration.TotalMilliseconds:F2} ms");
            Console.WriteLine($"Final version: {finalResult.Version}");

            if (finalValue == expectedFinalValue)
            {
                Console.WriteLine("\n[Success] Counter reached expected value! All concurrent increments succeeded.");
            }
            else
            {
                Console.WriteLine($"\n[Failed] Counter value mismatch! Expected {expectedFinalValue}, got {finalValue}");
            }
        }
        else
        {
            Console.WriteLine("\n[Failed] Counter key not found after all operations!");
        }
    }

    public static async Task DemonstrateOperationsOnMultipleKeys(IStore storeClient)
    {
        const int numberOfKeys = 1000;
        Console.WriteLine($"Demonstrating operations on {numberOfKeys} different keys");
        Console.WriteLine("Operations: PUT (create), GET (retrieve), PUT (update), PATCH (partial update)\n");

        var startTime = DateTime.UtcNow;
        int putCreates = 0;
        int putUpdates = 0;
        int gets = 0;
        int patches = 0;
        int errors = 0;

        // Phase 1: Create 1000 keys with PUT operations
        Console.WriteLine($"Phase 1: Creating {numberOfKeys} keys with PUT operations...");
        var createdKeys = new List<(string Key, int Version)>();
        
        for (int i = 1; i <= numberOfKeys; i++)
        {
            try
            {
                var key = $"bulk-key-{i:D4}";
                var value = $"Initial Value for Key {i}";
                var result = await storeClient.PutAsync(key, value);
                createdKeys.Add((key, result.Version));
                putCreates++;

                if (i % 100 == 0)
                {
                    Console.WriteLine($"   Created {i}/{numberOfKeys} keys...");
                }
            }
            catch (Exception ex)
            {
                errors++;
                Console.WriteLine($"   Error creating key {i}: {ex.Message}");
            }
        }

        Console.WriteLine($"Phase 1 completed: {putCreates} keys created, {errors} errors\n");

        // Phase 2: Retrieve all keys with GET operations
        Console.WriteLine($"Phase 2: Retrieving {numberOfKeys} keys with GET operations...");
        var retrievedCount = 0;
        var notFoundCount = 0;

        for (int i = 1; i <= numberOfKeys; i++)
        {
            try
            {
                var key = $"bulk-key-{i:D4}";
                var result = await storeClient.GetAsync(key);
                if (result != null)
                {
                    retrievedCount++;
                    gets++;
                }
                else
                {
                    notFoundCount++;
                }

                if (i % 100 == 0)
                {
                    Console.WriteLine($"   Retrieved {i}/{numberOfKeys} keys...");
                }
            }
            catch (Exception ex)
            {
                errors++;
                Console.WriteLine($"   Error retrieving key {i}: {ex.Message}");
            }
        }

        Console.WriteLine($"Phase 2 completed: {retrievedCount} keys retrieved, {notFoundCount} not found\n");

        // Phase 3: Update first 500 keys with PUT operations (with version check)
        Console.WriteLine($"Phase 3: Updating first 500 keys with PUT operations (version-checked)...");
        var updatedCount = 0;

        for (int i = 1; i <= 500; i++)
        {
            try
            {
                var key = $"bulk-key-{i:D4}";
                var keyInfo = createdKeys.FirstOrDefault(k => k.Key == key);
                if (keyInfo.Key != null)
                {
                    var newValue = $"Updated Value for Key {i}";
                    var result = await storeClient.PutAsync(key, newValue, keyInfo.Version);
                    updatedCount++;
                    putUpdates++;

                    if (i % 100 == 0)
                    {
                        Console.WriteLine($"   Updated {i}/500 keys...");
                    }
                }
            }
            catch (InvalidOperationException)
            {
                // Version conflict - expected in some cases, but shouldn't happen here
                errors++;
            }
            catch (Exception ex)
            {
                errors++;
                Console.WriteLine($"   Error updating key {i}: {ex.Message}");
            }
        }

        Console.WriteLine($"Phase 3 completed: {updatedCount} keys updated\n");

        // Phase 4: Patch last 500 keys with PATCH operations
        Console.WriteLine($"Phase 4: Patching last 500 keys with PATCH operations...");
        var patchedCount = 0;

        for (int i = 501; i <= numberOfKeys; i++)
        {
            try
            {
                var key = $"bulk-key-{i:D4}";
                var patchedValue = $"Patched Value for Key {i}";
                var result = await storeClient.PatchAsync(key, patchedValue);
                patchedCount++;
                patches++;

                if (i % 100 == 0)
                {
                    Console.WriteLine($"   Patched {i - 500}/500 keys...");
                }
            }
            catch (Exception ex)
            {
                errors++;
                Console.WriteLine($"   Error patching key {i}: {ex.Message}");
            }
        }

        Console.WriteLine($"Phase 4 completed: {patchedCount} keys patched\n");

        // Phase 5: Final verification - GET all keys again
        Console.WriteLine($"Phase 5: Final verification - Retrieving all {numberOfKeys} keys...");
        var verifiedCount = 0;

        for (int i = 1; i <= numberOfKeys; i++)
        {
            try
            {
                var key = $"bulk-key-{i:D4}";
                var result = await storeClient.GetAsync(key);
                if (result != null)
                {
                    verifiedCount++;
                }

                if (i % 200 == 0)
                {
                    Console.WriteLine($"   Verified {i}/{numberOfKeys} keys...");
                }
            }
            catch (Exception ex)
            {
                errors++;
            }
        }

        var endTime = DateTime.UtcNow;
        var duration = endTime - startTime;

        // Print summary
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("OPERATION SUMMARY");
        Console.WriteLine(new string('=', 60));
        Console.WriteLine($"Total Keys: {numberOfKeys}");
        Console.WriteLine($"PUT (Create): {putCreates}");
        Console.WriteLine($"GET (Retrieve): {gets}");
        Console.WriteLine($"PUT (Update): {putUpdates}");
        Console.WriteLine($"PATCH: {patches}");
        Console.WriteLine($"Final Verification: {verifiedCount} keys found");
        Console.WriteLine($"Errors: {errors}");
        Console.WriteLine($"Total Duration: {duration.TotalSeconds:F2} seconds");
        Console.WriteLine($"Average Operations per Second: {(putCreates + gets + putUpdates + patches) / duration.TotalSeconds:F2}");
        Console.WriteLine(new string('=', 60));

        if (errors == 0 && verifiedCount == numberOfKeys)
        {
            Console.WriteLine("\n[Success] All operations on 1000 keys completed successfully!");
        }
        else
        {
            Console.WriteLine($"\n[Warning] Completed with {errors} errors. {verifiedCount}/{numberOfKeys} keys verified.");
        }
    }
}