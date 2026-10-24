using System.Net;
using System.Text.Json;
using Mijia.Core.Abstractions;
using Mijia.Core.Models;
using Mijia.Core.Results;
using Mijia.Infrastructure.Serialization;

namespace Mijia.Infrastructure.Specifications;

/// <summary>Retrieves public MIoT metadata. Owns neither HttpClient nor account credentials.</summary>
public sealed class MiotSpecificationClient : ISpecificationProvider
{
    private readonly HttpClient _http;
    private readonly Uri _endpoint;
    private readonly TimeSpan _requestTimeout;

    public MiotSpecificationClient(HttpClient http, Uri? endpoint = null, TimeSpan? requestTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(http);
        _http = http;
        _endpoint = endpoint ?? new Uri("https://miot-spec.org/miot-spec-v2/instance");
        _requestTimeout = requestTimeout ?? TimeSpan.FromSeconds(30);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(_requestTimeout, TimeSpan.Zero, nameof(requestTimeout));
        if (!_endpoint.IsAbsoluteUri || _endpoint.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException("The specification endpoint must be an absolute HTTPS URI.", nameof(endpoint));
    }

    public async Task<Result<DeviceSpecification>> GetAsync(
        string specificationType, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(specificationType) ||
            !specificationType.StartsWith("urn:miot-spec-v2:device:", StringComparison.Ordinal))
            return Result.Fail<DeviceSpecification>(new ValidationError("specificationType", "A MIoT device specification URN is required."));

        var uri = new UriBuilder(_endpoint) { Query = "type=" + Uri.EscapeDataString(specificationType) }.Uri;
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Accept.ParseAdd("application/json");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(_requestTimeout);
        var requestToken = deadline.Token;

        try
        {
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
                requestToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var status = (int)response.StatusCode;
                return Result.Fail<DeviceSpecification>(new NetworkError(
                    "The specification service returned an unsuccessful response.", status,
                    response.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests || status >= 500));
            }

            await using var stream = await response.Content.ReadAsStreamAsync(requestToken).ConfigureAwait(false);
            var document = await JsonSerializer.DeserializeAsync(stream,
                MijiaJsonContext.Default.SpecificationDocument, requestToken).ConfigureAwait(false);
            if (document is null)
                return Result.Fail<DeviceSpecification>(new ProtocolError("The specification response is empty."));

            return Result.Ok(SpecificationMapper.Map(document, specificationType));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result.Fail<DeviceSpecification>(new NetworkError("The specification request timed out.", IsTransient: true));
        }
        catch (HttpRequestException exception)
        {
            return Result.Fail<DeviceSpecification>(new NetworkError("The specification service could not be reached.",
                exception.StatusCode is { } status ? (int)status : null, IsTransient: true));
        }
        catch (IOException)
        {
            return Result.Fail<DeviceSpecification>(new NetworkError("The specification response could not be read.", IsTransient: true));
        }
        catch (JsonException)
        {
            return Result.Fail<DeviceSpecification>(new ProtocolError("The service returned an invalid MIoT specification."));
        }
    }
}
