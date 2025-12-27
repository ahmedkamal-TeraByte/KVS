using System.Net;
using System.Text;
using System.Text.Json;

namespace KVS.Client.Test;

/// <summary>
/// Helper methods and classes for testing HttpClient-based code.
/// </summary>
public static class HttpClientTestHelpers
{
    /// <summary>
    /// Creates a mocked HttpClient with a configurable HttpMessageHandler.
    /// </summary>
    /// <param name="statusCode">The HTTP status code to return.</param>
    /// <param name="responseContent">The response content (string or object to serialize as JSON).</param>
    /// <param name="shouldThrow">Whether the handler should throw an exception.</param>
    /// <param name="baseAddress">The base address for the HttpClient.</param>
    /// <returns>A configured HttpClient instance.</returns>
    /// <remarks>
    /// Note: We cannot use Substitute.For&lt;HttpClient&gt;() because:
    /// 1. HttpClient methods (GetAsync, PutAsync, etc.) are NOT virtual - they can't be overridden/mocked
    /// 2. HttpClient internally uses HttpMessageHandler to make requests
    /// 3. The standard pattern is to mock HttpMessageHandler, not HttpClient
    /// 
    /// We also can't use Substitute.For&lt;HttpMessageHandler&gt;() directly because SendAsync() is protected.
    /// Therefore, we use a testable HttpMessageHandler that we can configure.
    /// This is the recommended approach for testing HttpClient.
    /// </remarks>
    public static HttpClient CreateMockedHttpClient(
        HttpStatusCode statusCode, 
        object? responseContent, 
        bool shouldThrow = false,
        Uri? baseAddress = null)
    {
        var handler = new TestableHttpMessageHandler();
        
        if (shouldThrow)
        {
            handler.ConfigureException(new HttpRequestException("Network error"));
        }
        else
        {
            var response = new HttpResponseMessage(statusCode);
            
            if (responseContent != null)
            {
                if (responseContent is string stringContent)
                {
                    response.Content = new StringContent(stringContent, Encoding.UTF8, "text/plain");
                }
                else
                {
                    // Match the serialization options used by StoreHttpClient
                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    var json = JsonSerializer.Serialize(responseContent, jsonOptions);
                    response.Content = new StringContent(json, Encoding.UTF8, "application/json");
                }
            }
            
            handler.ConfigureResponse(response);
        }

        var httpClient = new HttpClient(handler);
        if (baseAddress != null)
        {
            httpClient.BaseAddress = baseAddress;
        }

        return httpClient;
    }
}

/// <summary>
/// Testable HttpMessageHandler that can be configured for testing HttpClient.
/// </summary>
public class TestableHttpMessageHandler : HttpMessageHandler
{
    private HttpResponseMessage? _response;
    private Exception? _exception;
    
    /// <summary>
    /// Gets the URI of the last request made through this handler.
    /// </summary>
    public Uri? LastRequestUri { get; private set; }

    /// <summary>
    /// Configures the handler to return a specific response.
    /// </summary>
    /// <param name="response">The HttpResponseMessage to return.</param>
    public void ConfigureResponse(HttpResponseMessage response)
    {
        _response = response;
        _exception = null;
    }

    /// <summary>
    /// Configures the handler to throw a specific exception.
    /// </summary>
    /// <param name="exception">The exception to throw.</param>
    public void ConfigureException(Exception exception)
    {
        _exception = exception;
        _response = null;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequestUri = request.RequestUri;

        if (_exception != null)
        {
            throw _exception;
        }

        return Task.FromResult(_response ?? new HttpResponseMessage(HttpStatusCode.OK));
    }
}

