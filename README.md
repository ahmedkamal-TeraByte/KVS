# KVS - Key-Value Store

A distributed key-value store system built with .NET 8.0, consisting of a RESTful API server and a client library for interacting with the store.

## Table of Contents

1. [Running and Debugging KVS.Api](#section-1-running-and-debugging-kvsapi)
2. [Using KVS.Client in Your Application](#section-2-using-kvsclient-in-your-application)
3. [Using TestApplication](#section-3-using-testapplication)

---

## Section 1: Running and Debugging KVS.Api

### Prerequisites

- .NET 8.0 SDK (specified in `global.json`)
- Your IDE of choice (Visual Studio, Rider, VS Code, etc.)

### Running KVS.Api

#### Option 1: Using .NET CLI

Navigate to the `KVS.Api` directory and run:

```bash
cd KVS.Api
dotnet run
```

The API will start on `http://localhost:5145` by default.

#### Option 2: Using Command-Line Arguments

You can specify a custom host and port:

```bash
cd KVS.Api
dotnet run --host localhost --port 5000
```

This will start the API on `http://localhost:5000`.

#### Option 3: Using Launch Settings

The project includes launch profiles in `Properties/launchSettings.json`:

- **http**: Runs on `http://localhost:5145` with Swagger UI
- **https**: Runs on both `https://localhost:7011` and `http://localhost:5145` with Swagger UI
- **IIS Express**: Runs using IIS Express

### Debugging KVS.Api

#### Using Visual Studio or Rider

1. Open the solution file `KVS.sln`
2. Set `KVS.Api` as the startup project
3. Press `F5` or click the Debug button
4. The API will start in debug mode with breakpoints enabled

#### Using VS Code

1. Open the workspace in VS Code
2. Navigate to `KVS.Api` directory
3. Use the built-in debugger or run `dotnet run` from the terminal
4. Attach the debugger if needed

### Accessing the API

Once running, you can:

- **Swagger UI**: Navigate to `http://localhost:5145/swagger` (in Development mode) to view and test the API endpoints
- **API Endpoints**: 
  - `GET /kv` - Get all keys
  - `GET /kv/{key}` - Get a specific key-value pair
  - `PUT /kv/{key}?ifVersion={version}` - Create or update a key-value pair
  - `PATCH /kv/{key}?ifVersion={version}` - Partially update a key-value pair

### Configuration

The API uses an in-memory store by default. Configuration can be found in:
- `appsettings.json` - General application settings
- `appsettings.Development.json` - Development-specific settings

---

## Section 2: Using KVS.Client in Your Application

### Adding KVS.Client to Your Project

#### Option 1: Project Reference (Same Solution)

If your application is in the same solution, add a project reference:

```xml
<ItemGroup>
  <ProjectReference Include="..\KVS.Client\KVS.Client.csproj" />
</ItemGroup>
```

#### Option 2: NuGet Package

(Not yet done) If KVS.Client is published as a NuGet package:

```bash
dotnet add package KVS.Client
```

### Basic Usage

#### 1. Create a StoreConfig

First, define your store configuration. You can do this programmatically or via configuration:

```csharp
using KVS.Client.Models;

var storeConfig = new StoreConfig
{
    StoreServers = new List<StoreServer>
    {
        new StoreServer
        {
            NodeId = "Node 1",
            Url = "localhost",
            Port = 5000
        },
        new StoreServer
        {
            NodeId = "Node 2",
            Url = "localhost",
            Port = 5001
        }
    },
    TimeoutSeconds = 30
};
```

#### 2. Initialize the StoreClient

```csharp
using KVS.Client;
using KVS.Client.Contracts;

IStoreClient storeClient = StoreClient.GetInstance(storeConfig);
```

Alternatively, if you have a custom `IStoreHttpClientFactory`:

```csharp
IStoreHttpClientFactory factory = new StoreHttpClientFactory(storeConfig);
IStoreClient storeClient = StoreClient.GetInstance(factory);
```

#### 3. Using Configuration Files

You can also load configuration from `appsettings.json`:

```csharp
using Microsoft.Extensions.Configuration;
using KVS.Client.Models;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var storeConfig = configuration.GetSection("StoreConfig").Get<StoreConfig>();
IStoreClient storeClient = StoreClient.GetInstance(storeConfig);
```

Your `appsettings.json` should look like:

```json
{
  "StoreConfig": {
    "StoreServers": [
      {
        "NodeId": "Node 1",
        "Url": "localhost",
        "Port": 5000
      },
      {
        "NodeId": "Node 2",
        "Url": "localhost",
        "Port": 5001
      }
    ],
    "TimeoutSeconds": 30
  }
}
```

### Available Operations

#### Get a Value

```csharp
var result = await storeClient.GetAsync("my-key");
if (result != null)
{
    Console.WriteLine($"Key: {result.Key}");
    Console.WriteLine($"Value: {result.Value}");
    Console.WriteLine($"Version: {result.Version}");
}
```

#### Put (Create/Update) a Value

```csharp
// Create or update without version check
var result = await storeClient.PutAsync("my-key", "my-value");

// Update with version check (optimistic concurrency)
var result = await storeClient.PutAsync("my-key", "updated-value", ifVersion: 1);
```

#### Patch (Partial Update) a Value

```csharp
// Patch without version check
var result = await storeClient.PatchAsync("my-key", "patched-value");

// Patch with version check
var result = await storeClient.PatchAsync("my-key", "patched-value", ifVersion: 1);
```

#### Get All Keys

```csharp
var allKeys = await storeClient.GetAllKeysAsync();
foreach (var key in allKeys)
{
    Console.WriteLine(key);
}
```

#### Get All Keys from All Nodes

```csharp
var nodeKeys = await storeClient.GetAllNodeKeysAsync();
foreach (var nodeKey in nodeKeys)
{
    Console.WriteLine($"Node: {nodeKey.NodeId}, Key: {nodeKey.Key}");
}
```

### Error Handling

The client methods may throw exceptions:

- `ArgumentNullException`: When a key is null or empty
- `InvalidOperationException`: When a version conflict occurs (optimistic concurrency failure)
- `HttpRequestException`: When HTTP requests fail

Example error handling:

```csharp
try
{
    var result = await storeClient.PutAsync("my-key", "value", ifVersion: 999);
}
catch (InvalidOperationException)
{
    Console.WriteLine("Version conflict - the key was modified by another operation");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
```

### Dependency Injection

If you're using dependency injection, you can register the client as a singleton:

```csharp
services.AddSingleton<IStoreClient>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var storeConfig = configuration.GetSection("StoreConfig").Get<StoreConfig>();
    return StoreClient.GetInstance(storeConfig);
});
```

---

## Section 3: Using TestApplication

The `TestApplication` is a demonstration application that showcases various features of the KVS system.

### Prerequisites

Before running the TestApplication, ensure:

1. **KVS.Api is running**: You need at least one instance of KVS.Api running. For multi-node demonstrations, run multiple instances on different ports.

2. **Configuration is set up**: The `TestApplication` requires an `appsettings.json` file with the `StoreConfig` section.

### Setting Up TestApplication

#### 1. Configure appsettings.json

Ensure `TestApplication/appsettings.json` contains your store server configuration:

```json
{
  "StoreConfig": {
    "StoreServers": [
      {
        "NodeId": "Node 1",
        "Url": "localhost",
        "Port": 5000
      },
      {
        "NodeId": "Node 2",
        "Url": "localhost",
        "Port": 5001
      }
    ],
    "TimeoutSeconds": 30
  }
}
```

#### 2. Start KVS.Api Instances

For a single-node test, start one instance:

```bash
cd KVS.Api
dotnet run --host localhost --port 5000
```

For multi-node testing, start multiple instances in separate terminals:

**Terminal 1:**
```bash
cd KVS.Api
dotnet run --host localhost --port 5000
```

**Terminal 2:**
```bash
cd KVS.Api
dotnet run --host localhost --port 5001
```

#### 3. Run TestApplication

```bash
cd TestApplication
dotnet run
```

### What TestApplication Demonstrates

The TestApplication runs three main demonstrations:

#### 1. Simple Operations (`DemonstrateSimpleOperations`)

Demonstrates basic CRUD operations:
- PUT operation to create a key-value pair
- GET operation to retrieve a value
- PUT operation with version check for optimistic concurrency
- PATCH operation for partial updates
- Error handling for version conflicts
- Handling non-existent keys

#### 2. Concurrent Counter Increment (`DemonstrateConcurrentCounterIncrement`)

Demonstrates concurrent operations and optimistic concurrency control:
- Initializes a counter to 0
- Runs 3 concurrent clients, each performing 100 increments
- Uses version checking to handle concurrent updates
- Verifies the final counter value (expected: 300)
- Shows retry logic for version conflicts

#### 3. Operations on Multiple Keys (`DemonstrateOperationsOnMultipleKeys`)

Demonstrates bulk operations:
- Creates 1000 keys with PUT operations
- Retrieves all 1000 keys with GET operations
- Updates the first 500 keys with version-checked PUT operations
- Patches the last 500 keys with PATCH operations
- Verifies all keys at the end
- Provides performance metrics (operations per second, duration)

#### 4. Multi-Node Key Retrieval

After the demonstrations, the application fetches all keys from all configured nodes and displays them with their node IDs.

### Expected Output

The TestApplication provides detailed console output showing:
- Progress of each operation
- Success/failure status for each demonstration
- Performance metrics
- Final verification results

### Troubleshooting

**Issue: "StoreConfig section is missing or invalid"**
- Ensure `appsettings.json` exists in the `TestApplication` directory
- Verify the JSON structure matches the expected format

**Issue: Connection errors**
- Ensure KVS.Api instances are running on the ports specified in `appsettings.json`
- Check that the ports are not already in use
- Verify firewall settings if testing across machines

**Issue: Version conflicts in concurrent operations**
- This is expected behavior - the application includes retry logic
- If you see many retries, consider increasing the delay between operations

---

## Project Structure

```
KVS/
├── KVS.Api/              # RESTful API server
├── KVS.Client/           # Client library for applications
├── KVS.Server/           # Server-side store implementation
├── KVS.Structure/        # Shared interfaces and models
├── TestApplication/      # Demonstration application
└── Tests/                # Unit tests
```


