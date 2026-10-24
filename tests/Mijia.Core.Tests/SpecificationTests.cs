using System.Net;
using System.Text;
using Mijia.Core.Models;
using Mijia.Core.Results;
using Mijia.Infrastructure.Specifications;

namespace Mijia.Core.Tests;

public sealed class SpecificationTests
{
    // Synthetic protocol fixture; no third-party implementation or account data.
    private const string Document = """
        {
          "type": "urn:miot-spec-v2:device:light:0000A001:example-light:1",
          "description": "Example light",
          "services": [{
            "iid": 2, "type": "urn:miot-spec-v2:service:light:00007802:example:1", "description": "Light",
            "properties": [{
              "iid": 1, "type": "urn:miot-spec-v2:property:brightness:0000000D:example:1",
              "description": "Brightness", "format": "uint8", "unit": "percentage",
              "access": ["read", "write", "notify"], "value-range": [0, 100, 1],
              "value-list": [{"value": 100, "description": "Full"}]
            }],
            "actions": [{"iid": 1, "type": "urn:miot-spec-v2:action:apply:00000001:example:1",
              "description": "Apply", "in": [1], "out": [1]}],
            "events": [{"iid": 1, "type": "urn:miot-spec-v2:event:changed:00000001:example:1",
              "description": "Changed", "arguments": [1]}]
          }]
        }
        """;

    [Fact]
    public async Task MapsOfficialSchemaIncludingActionInputsAndEvents()
    {
        Uri? requestUri = null;
        using var http = new HttpClient(new StubHttp((request, _) =>
        {
            requestUri = request.RequestUri;
            return Task.FromResult(Json(Document));
        }));
        var result = await new MiotSpecificationClient(http).GetAsync(TestData.Type);
        Assert.True(result is Success<DeviceSpecification>);
        var spec = result.Match(value => value, _ => throw new InvalidOperationException());
        var property = spec.FindProperty(new PropertyId(2, 1))!;
        Assert.Equal(PropertyAccess.Read | PropertyAccess.Write | PropertyAccess.Notify, property.Access);
        Assert.Equal("percentage", property.Unit);
        Assert.True(property.AllowedValues[0].Value is IntegerValue { Value: 100 });
        Assert.Equal(1, Assert.Single(spec.FindAction(new ActionId(2, 1))!.InputPropertyIds));
        Assert.Equal(1, Assert.Single(spec.FindAction(new ActionId(2, 1))!.OutputPropertyIds));
        Assert.Equal(1, Assert.Single(spec.Services[0].Events[0].ArgumentPropertyIds));
        Assert.Equal(TestData.Type, Uri.UnescapeDataString(requestUri!.Query[6..]));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"services\":[]}")]
    public async Task MalformedResponsesAreProtocolErrors(string document)
    {
        using var http = new HttpClient(new StubHttp((_, _) => Task.FromResult(Json(document))));
        Assert.True(await new MiotSpecificationClient(http).GetAsync(TestData.Type) is Failure { Error: ProtocolError });
    }

    [Fact]
    public async Task MissingActionReferenceIsProtocolError()
    {
        var document = Document.Replace("\"in\": [1]", "\"in\": [99]", StringComparison.Ordinal);
        using var http = new HttpClient(new StubHttp((_, _) => Task.FromResult(Json(document))));
        Assert.True(await new MiotSpecificationClient(http).GetAsync(TestData.Type) is Failure { Error: ProtocolError });
    }

    [Fact]
    public async Task NullEnumerationEntryIsProtocolError()
    {
        var document = Document.Replace("{\"value\": 100, \"description\": \"Full\"}", "null", StringComparison.Ordinal);
        using var http = new HttpClient(new StubHttp((_, _) => Task.FromResult(Json(document))));
        Assert.True(await new MiotSpecificationClient(http).GetAsync(TestData.Type) is Failure { Error: ProtocolError });
    }

    [Fact]
    public async Task Http429IsTransientWithoutExposingResponseBody()
    {
        using var http = new HttpClient(new StubHttp((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("private upstream data")
        })));
        var result = await new MiotSpecificationClient(http).GetAsync(TestData.Type);
        Assert.True(result is Failure { Error: NetworkError { HttpStatusCode: 429, IsTransient: true } });
    }

    [Fact]
    public async Task RequestTimeoutIsAnExpectedNetworkFailure()
    {
        using var http = new HttpClient(new StubHttp(async (_, token) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return Json(Document);
        }));
        var client = new MiotSpecificationClient(http, requestTimeout: TimeSpan.FromMilliseconds(20));
        Assert.True(await client.GetAsync(TestData.Type) is Failure { Error: NetworkError { IsTransient: true } });
    }

    [Fact]
    public async Task CallerCancellationRemainsAnException()
    {
        using var cancellation = new CancellationTokenSource();
        using var http = new HttpClient(new StubHttp((_, token) =>
        {
            cancellation.Cancel();
            token.ThrowIfCancellationRequested();
            return Task.FromResult(Json(Document));
        }));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new MiotSpecificationClient(http).GetAsync(TestData.Type, cancellation.Token));
    }

    [Fact]
    public async Task ConcurrentCacheRequestsShareOneFetchAndExpire()
    {
        var time = new ManualTimeProvider();
        var source = new StubSpecifications
        {
            Handler = async (_, _) =>
            {
                await Task.Yield();
                return Result.Ok(TestData.Specification());
            }
        };
        using var cache = new CachedSpecificationProvider(source, TimeSpan.FromMinutes(1), timeProvider: time);
        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => cache.GetAsync(TestData.Type)));
        Assert.Equal(1, source.Calls);
        time.Now = time.Now.AddMinutes(1);
        await cache.GetAsync(TestData.Type);
        Assert.Equal(2, source.Calls);
        await cache.InvalidateAsync(TestData.Type);
        await cache.GetAsync(TestData.Type);
        Assert.Equal(3, source.Calls);
    }

    [Fact]
    public async Task CacheDoesNotRetainFailures()
    {
        var source = new StubSpecifications
        {
            Handler = (_, _) => Task.FromResult(Result.Fail<DeviceSpecification>(new NetworkError("Unavailable")))
        };
        using var cache = new CachedSpecificationProvider(source);
        Assert.True(await cache.GetAsync(TestData.Type) is Failure);
        Assert.True(await cache.GetAsync(TestData.Type) is Failure);
        Assert.Equal(2, source.Calls);
    }

    private static HttpResponseMessage Json(string document) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(document, Encoding.UTF8, "application/json")
    };

    private sealed class StubHttp(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            handler(request, cancellationToken);
    }
}
