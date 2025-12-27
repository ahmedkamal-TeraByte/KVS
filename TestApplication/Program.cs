using KVS.Client;
using KVS.Client.Contracts;
using KVS.Client.Models;
using Microsoft.Extensions.Configuration;

namespace TestApplication;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("KVS Test Application");
        Console.WriteLine("===================\n");

        try
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var storeConfig = configuration.GetSection("StoreConfig").Get<StoreConfig>();

            if (storeConfig == null)
            {
                throw new InvalidOperationException(
                    "StoreConfig section is missing or invalid in appsettings.json. " +
                    "Please ensure StoreConfig with StoreServers and TimeoutSeconds is configured.");
            }
            
            // Initialize services
            var storeClient = StoreClient.GetInstance(storeConfig);

            Console.WriteLine("StoreClient initialized successfully.\n");

            // Demonstrate various operations
            await DemonstrateOperations(storeClient);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            Environment.Exit(1);
        }
    }

    private static async Task DemonstrateOperations(IStoreClient storeClient)
    {
        await Demonstration.DemonstrateSimpleOperations(storeClient);
        await Demonstration.DemonstrateConcurrentCounterIncrement(storeClient);
        await Demonstration.DemonstrateOperationsOnMultipleKeys(storeClient);

        var result = await storeClient.GetAllNodeKeysAsync();
        Console.WriteLine($"Total keys fetched from all nodes: {result.Count}");
        foreach (var node in result)
        {
            Console.WriteLine($"{node.NodeId}:{node.Key}");
        }
    }
}
