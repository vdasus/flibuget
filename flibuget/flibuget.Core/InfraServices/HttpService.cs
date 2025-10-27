using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;

namespace flibuget.Core.InfraServices;

/* TODO Additional Recommendations:
   •	Configure named HttpClients in your DI registration with sensible default timeouts
   •	Consider using HttpCompletionOption.ResponseHeadersRead if you're dealing with large responses
   •	Add retry policies using Polly if not already implemented  */

public class HttpService(IHttpClientFactory clientFactory, ILogger<HttpService> logger)
    : IWebService
{
    private readonly IHttpClientFactory _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
    private readonly ILogger<HttpService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    //TODO take timeout from config
    private const int DEFAULT_TIMEOUT_SEC = 30 * 60;

    public async Task<string> MakeGetRequestAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("Starting GET request to {Uri}", uri);

        var client = _clientFactory.CreateClient();

        // Use CancellationTokenSource for timeout instead of modifying client.Timeout
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(DEFAULT_TIMEOUT_SEC));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        using var response = await client.GetAsync(uri, linkedCts.Token).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("GET request failed with status code {StatusCode} for {Uri}", response.StatusCode, uri);
            throw new HttpRequestException($"Status code {response.StatusCode} returned on get");
        }

        _logger.LogTrace("GET request completed successfully for {Uri} with status code {StatusCode}", uri, response.StatusCode);
        return await response.Content.ReadAsStringAsync(linkedCts.Token).ConfigureAwait(false);
    }

    #region JSON requests

    public Task<string> MakeJsonPostRequestAsync(Uri uri, string data, int timeout = DEFAULT_TIMEOUT_SEC, CancellationToken cancellationToken = default)
    {
        return MakePostRequestAsync(uri, data, timeout, "application/json", "application/json", null, null, null, cancellationToken);
    }

    public Task<string> MakeJsonPostRequestWithBearerAsync(Uri uri, string data, string bearerToken, int timeout = 240,
        CancellationToken cancellationToken = default)
    {
        return MakePostRequestAsync(uri, data, timeout, "application/json", "application/json", "Bearer", bearerToken, null, cancellationToken);
    }

    public Task<string> MakeJsonPostRequestWithApiKeyAsync(Uri uri, string data, string apiKey, int timeout = 240,
        CancellationToken cancellationToken = default)
    {
        return MakePostRequestAsync(uri, data, timeout, "application/json", "application/json", null, null, apiKey, cancellationToken);
    }

    public Task<string> MakeJsonPostRequestWithInternalAsync(Uri uri, string data, string internalKey, int timeout = 240,
        CancellationToken cancellationToken = default)
    {
        return MakePostRequestAsync(uri, data, timeout, "application/json", "application/json", "internal", internalKey,
            null, cancellationToken);
    }

    #endregion

    #region XML requests

    public Task<string> MakeXmlPostRequestAsync(Uri uri, string data, int timeout = DEFAULT_TIMEOUT_SEC, CancellationToken cancellationToken = default)
    {
        return MakePostRequestAsync(uri, data, timeout, "application/xml", "application/xml", null, null, null, cancellationToken);
    }

    public Task<string> MakeXmlPostRequestWithBearerAsync(Uri uri, string data, string bearerToken, int timeout = 240,
        CancellationToken cancellationToken = default)
    {
        return MakePostRequestAsync(uri, data, timeout, "application/xml", "application/xml", "Bearer", bearerToken, null, cancellationToken);
    }

    public Task<string> MakeXmlPostRequestWithApiKeyAsync(Uri uri, string data, string apiKey, int timeout = 240,
        CancellationToken cancellationToken = default)
    {
        return MakePostRequestAsync(uri, data, timeout, "application/xml", "application/xml", null, null, apiKey,
            cancellationToken);
    }

    #endregion

    private async Task<string> MakePostRequestAsync(
        Uri uri,
        string data,
        int timeout = DEFAULT_TIMEOUT_SEC,
        string acceptHeader = "application/xml",
        string contentTypeHeader = "application/xml",
        string? tokenName = null,
        string? bearerOrInternalToken = null,
        string? apiKey = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(uri);
        if (string.IsNullOrWhiteSpace(data))
            throw new ArgumentException("Data cannot be null or whitespace.", nameof(data));

        try
        {
            var client = _clientFactory.CreateClient();

            // Use CancellationTokenSource for timeout instead of modifying client.Timeout
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            using var request = new HttpRequestMessage(HttpMethod.Post, uri);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(acceptHeader));

            // Set authentication based on provided parameters
            if (!string.IsNullOrWhiteSpace(bearerOrInternalToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue(tokenName ?? "Bearer", bearerOrInternalToken);
                _logger.LogTrace("Using provided Bearer token for authentication");
            }
            else if (!string.IsNullOrWhiteSpace(apiKey))
            {
                request.Headers.Add("X-API-Key", apiKey);
                _logger.LogTrace("Using provided API key for authentication");
            }

            request.Content = new StringContent(data, Encoding.UTF8, contentTypeHeader);

            _logger.LogTrace("Sending HTTP POST request to {Uri} with ContentType {ContentType} and Accept {Accept} (Timeout: {TimeoutSeconds}s)",
                uri, contentTypeHeader, acceptHeader, timeout);

            using var response = await client.SendAsync(request, linkedCts.Token).ConfigureAwait(false);
            
            _logger.LogTrace("HTTP POST request completed for {Uri} with status code {StatusCode}", uri, response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync(linkedCts.Token).ConfigureAwait(false);
            }

            _logger.LogError("HTTP POST request failed for {Uri} with status code {StatusCode}", uri, response.StatusCode);
            throw new HttpRequestException($"Status code {response.StatusCode} returned on post");
        }
        catch (HttpRequestException httpRequestException)
        {
            _logger.LogError(httpRequestException, "HTTP request error for {Uri} in {MethodName}",
                uri, nameof(MakePostRequestAsync));
            throw;
        }
        catch (OperationCanceledException operationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Timeout occurred (not user cancellation)
            _logger.LogError(operationCanceledException, "HTTP request timed out for {Uri} after {TimeoutSeconds}s in {MethodName}",
                uri, timeout, nameof(MakePostRequestAsync));
            throw new TimeoutException($"The request timed out after {timeout} seconds.", operationCanceledException);
        }
        catch (TaskCanceledException taskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Timeout occurred (not user cancellation)
            _logger.LogError(taskCanceledException, "HTTP request timed out for {Uri} after {TimeoutSeconds}s in {MethodName}",
                uri, timeout, nameof(MakePostRequestAsync));
            throw new TimeoutException($"The request timed out after {timeout} seconds.", taskCanceledException);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred for {Uri} in {MethodName}", uri, nameof(MakePostRequestAsync));
            throw;
        }
    }
}