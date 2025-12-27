using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using KVS.Client.Contracts;
using KVS.Client.Models;
using KVS.Structure.Models;

namespace KVS.Client;

internal class StoreHttpClient : IStoreHttpClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public StoreHttpClient(StoreServer server, int timeoutSeconds = 5)
    {
        if (string.IsNullOrEmpty(server.Url) || server.Port <= 0)
            throw new ArgumentException(
                $"'{nameof(server.Url)}' cannot be null or empty and '{nameof(server.Port)}' must be greater than 0",
                nameof(server));

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri($"http://{server.Url}:{server.Port}/"),
            Timeout = TimeSpan.FromSeconds(timeoutSeconds)
        };
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        NodeId = server.NodeId ?? $"{server.Url}:{server.Port}";
    }

    public StoreHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        NodeId = Guid.NewGuid().ToString();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public string NodeId { get; }

    public async Task<StoreValue?> GetAsync(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key), "Key cannot be null or empty.");

        var url = BuildUrlWithQuery(key);
        var response = await ExecuteHttpRequestAsync(() => _httpClient.GetAsync(url), "get");

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            response.Dispose();
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await DeserializeResponseAsync<StoreValue>(response);
    }

    public async Task<ICollection<string>> GetAllKeysAsync()
    {
        var url = BuildUrlWithQuery();
        var response = await ExecuteHttpRequestAsync(() => _httpClient.GetAsync(url), "patch");

        await HandleResponseErrorsAsync(response);

        return await DeserializeResponseAsync<ICollection<string>>(response);
    }

    public async Task<StoreValue> PutAsync(string key, string value, int ifVersion = 0)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key), "Key cannot be null or empty.");

        var url = BuildUrlWithQuery(key, ifVersion);
        var jsonValue = JsonSerializer.Serialize(value, _jsonOptions);
        var content = new StringContent(jsonValue, Encoding.UTF8, "application/json");
        var response = await ExecuteHttpRequestAsync(() => _httpClient.PutAsync(url, content), "put");

        await HandleResponseErrorsAsync(response);

        return await DeserializeResponseAsync<StoreValue>(response);
    }

    public async Task<StoreValue> PatchAsync(string key, string delta, int ifVersion = 0)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key), "Key cannot be null or empty.");

        var url = BuildUrlWithQuery(key, ifVersion);
        var jsonValue = JsonSerializer.Serialize(delta, _jsonOptions);
        var content = new StringContent(jsonValue, Encoding.UTF8, "application/json");
        var response = await ExecuteHttpRequestAsync(() => _httpClient.PatchAsync(url, content), "patch");

        await HandleResponseErrorsAsync(response);

        return await DeserializeResponseAsync<StoreValue>(response);
    }

    private static string BuildUrlWithQuery(string key = "", int ifVersion = 0)
    {
        var response = "kv/";
        if (!string.IsNullOrEmpty(key))
        {
            response = $"{response}{Uri.EscapeDataString(key)}";
        }

        if (ifVersion != 0)
        {
            return $"{response}?ifVersion={ifVersion}";
        }

        return response;
    }

    private async Task<HttpResponseMessage> ExecuteHttpRequestAsync(Func<Task<HttpResponseMessage>> httpCall, string operation)
    {
        HttpResponseMessage? response = null;
        try
        {
            response = await httpCall();
            return response;
        }
        catch (HttpRequestException ex)
        {
            response?.Dispose();
            throw new HttpRequestException(
                $"Failed to {operation} value on server {NodeId}. {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            response?.Dispose();
            throw new TimeoutException($"Request to {operation} on server {NodeId} timed out.", ex);
        }
        catch (ObjectDisposedException ex)
        {
            response?.Dispose();
            throw new InvalidOperationException($"HttpClient for server {NodeId} has been disposed.", ex);
        }
        catch (Exception ex)
        {
            response?.Dispose();
            throw new InvalidOperationException(
                $"Unexpected error occurred while {operation}ing on server {NodeId}. {ex.Message}", ex);
        }
    }

    private async Task<T> DeserializeResponseAsync<T>(HttpResponseMessage response)
    {
        try
        {
            var result = await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
            return result!;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize response from server {NodeId}. {ex.Message}", ex);
        }
        finally
        {
            response.Dispose();
        }
    }

    private static async Task HandleResponseErrorsAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        try
        {
            switch (response.StatusCode)
            {
                case System.Net.HttpStatusCode.BadRequest:
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    throw new Exception(errorMessage);

                case System.Net.HttpStatusCode.Conflict:
                    throw new InvalidOperationException("Version mismatch");

                default:
                    response.EnsureSuccessStatusCode();
                    break;
            }
        }
        catch (HttpRequestException ex)
        {
            throw new HttpRequestException(
                $"HTTP error occurred while processing response. {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new TimeoutException($"Timeout occurred while reading error response.", ex);
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }
}