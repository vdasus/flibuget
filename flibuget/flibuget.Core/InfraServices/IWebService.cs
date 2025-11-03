namespace flibuget.Core.InfraServices;

public interface IWebService
{
    // TODO remove constants from default timeout
    Task<string> MakeGetRequestAsync(Uri uri, CancellationToken cancellationToken = default);
    Task<byte[]> MakeGetByteArrayAsync(Uri uri, CancellationToken cancellationToken = default);

    Task<string> MakeJsonPostRequestAsync(Uri uri, string data, int timeout = 30 * 60, CancellationToken cancellationToken = default);
    Task<string> MakeJsonPostRequestWithBearerAsync(Uri uri, string data, string bearerToken, int timeout = 240, CancellationToken cancellationToken = default);
    Task<string> MakeJsonPostRequestWithApiKeyAsync(Uri uri, string data, string apiKey, int timeout = 240, CancellationToken cancellationToken = default);
    Task<string> MakeJsonPostRequestWithInternalAsync(Uri uri, string data, string internalKey, int timeout = 240, CancellationToken cancellationToken = default);

    Task<string> MakeXmlPostRequestAsync(Uri uri, string data, int timeout = 30 * 60, CancellationToken cancellationToken = default);
    Task<string> MakeXmlPostRequestWithBearerAsync(Uri uri, string data, string bearerToken, int timeout = 240, CancellationToken cancellationToken = default);
    Task<string> MakeXmlPostRequestWithApiKeyAsync(Uri uri, string data, string apiKey, int timeout = 240, CancellationToken cancellationToken = default);
}