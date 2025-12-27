using KVS.Server;
using KVS.Structure;
using KVS.Structure.Models;

namespace KVS.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Parse host and port from command-line arguments
        var host = ParseHostFromArgs(args);
        var port = ParsePortFromArgs(args);
        
        if (port.HasValue)
        {
            builder.WebHost.UseUrls($"http://{host ?? "localhost"}:{port.Value}");
        }

        // Add services to the container.

        builder.Services.AddControllers();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var storeFactory = new StoreFactory();
        builder.Services.AddSingleton<IStore>(storeFactory.GetStoreInstance(StoreType.InMemory));
        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        
        app.UseAuthorization();
        
        app.MapControllers();

        app.Run();
    }

    private static string? ParseHostFromArgs(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--host" && i + 1 < args.Length)
            {
                var host = args[i + 1];
                if (!string.IsNullOrWhiteSpace(host))
                {
                    return host;
                }
            }
        }
        return null;
    }

    private static int? ParsePortFromArgs(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--port" && i + 1 < args.Length)
            {
                if (int.TryParse(args[i + 1], out var port) && port is > 0 and <= 65535)
                {
                    return port;
                }
            }
        }
        return null;
    }
}