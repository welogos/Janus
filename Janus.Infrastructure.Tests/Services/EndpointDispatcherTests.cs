using System.Net;
using System.Text;
using Janus.Application.Exceptions;
using Janus.Application.Models.Dispatching;
using Janus.Domain.Enums;
using Janus.Domain.Models;
using Janus.Infrastructure.Services.Dispatching;
using Microsoft.Extensions.Logging.Abstractions;

namespace Janus.Infrastructure.Tests.Services;

public class EndpointDispatcherTests
{
    [Fact]
    public async Task WhenRequestIsDispatchedHasForwardedSafeDataAndRemovedUnsafeHeaders()
    {
        var capture = new RequestCapture();
        var handler = new ControlledHandler(async (request, cancellationToken) =>
        {
            await capture.CaptureAsync(request, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var dispatcher = CreateDispatcher(handler);
        var request = CreateRequest(
            queryString: "?page=2&filter=active",
            body: """{"name":"Janus"}""",
            headers: new Dictionary<string, IReadOnlyList<string>>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["Accept"] = ["application/json"],
                ["Content-Type"] = ["application/json; charset=utf-8"],
                ["Authorization"] = ["Bearer secret-token"],
                ["Cookie"] = ["session=secret"],
                ["Host"] = ["attacker.example"],
                ["Connection"] = ["keep-alive"],
                ["X-Internal-Token"] = ["internal-secret"],
                ["X-Correlation-ID"] = ["untrusted-value"],
            });

        await dispatcher.DispatchAsync(CreateEndpoint(), request, CancellationToken.None);

        Assert.Equal(HttpMethod.Post, capture.Method);
        Assert.Equal(
            "http://hermes:8080/internal/messages?configured=true&page=2&filter=active",
            capture.Uri!.ToString());
        Assert.Equal("""{"name":"Janus"}""", capture.Body);
        Assert.Equal("application/json; charset=utf-8", capture.ContentType);
        Assert.Equal("application/json", capture.Headers["Accept"].Single());
        Assert.Equal("correlation-123", capture.Headers["X-Correlation-ID"].Single());
        Assert.DoesNotContain("Authorization", capture.Headers.Keys);
        Assert.DoesNotContain("Cookie", capture.Headers.Keys);
        Assert.DoesNotContain("Host", capture.Headers.Keys);
        Assert.DoesNotContain("Connection", capture.Headers.Keys);
        Assert.DoesNotContain("X-Internal-Token", capture.Headers.Keys);
    }

    [Fact]
    public async Task WhenDestinationRespondsHasPreservedStatusBodyAndOnlySafeHeaders()
    {
        var handler = new ControlledHandler((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.UnprocessableEntity)
            {
                Content = new StringContent("validation failed", Encoding.UTF8, "text/plain"),
            };
            response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"v1\"");
            response.Headers.Server.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("private-service", "1.0"));
            response.Headers.TryAddWithoutValidation("Set-Cookie", "session=secret");
            response.Headers.Connection.Add("keep-alive");
            return Task.FromResult(response);
        });
        var dispatcher = CreateDispatcher(handler);

        var response = await dispatcher.DispatchAsync(
            CreateEndpoint(),
            CreateRequest(),
            CancellationToken.None);

        Assert.Equal(422, response.StatusCode);
        Assert.Equal("validation failed", Encoding.UTF8.GetString(response.Body.Span));
        Assert.Equal("text/plain; charset=utf-8", response.Headers["Content-Type"].Single());
        Assert.Equal("\"v1\"", response.Headers["ETag"].Single());
        Assert.DoesNotContain("Server", response.Headers.Keys);
        Assert.DoesNotContain("Set-Cookie", response.Headers.Keys);
        Assert.DoesNotContain("Connection", response.Headers.Keys);
    }

    [Fact]
    public async Task WhenDestinationReturnsNoContentHasReturnedEmptyBody()
    {
        var handler = new ControlledHandler((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.NoContent)));
        var dispatcher = CreateDispatcher(handler);

        var response = await dispatcher.DispatchAsync(
            CreateEndpoint(),
            CreateRequest(),
            CancellationToken.None);

        Assert.Equal(204, response.StatusCode);
        Assert.True(response.Body.IsEmpty);
    }

    [Fact]
    public async Task WhenDestinationTimesOutHasThrownTimeoutException()
    {
        var handler = new ControlledHandler((_, _) =>
            throw new TaskCanceledException("Destination timeout."));
        var dispatcher = CreateDispatcher(handler);

        await Assert.ThrowsAsync<EndpointDispatchTimeoutException>(() =>
            dispatcher.DispatchAsync(
                CreateEndpoint(),
                CreateRequest(),
                CancellationToken.None));
    }

    [Fact]
    public async Task WhenDestinationConnectionFailsHasThrownUpstreamException()
    {
        var handler = new ControlledHandler((_, _) =>
            throw new HttpRequestException("Connection refused."));
        var dispatcher = CreateDispatcher(handler);

        await Assert.ThrowsAsync<EndpointUpstreamException>(() =>
            dispatcher.DispatchAsync(
                CreateEndpoint(),
                CreateRequest(),
                CancellationToken.None));
    }

    [Fact]
    public async Task WhenDestinationResponseCannotBeReadHasThrownUpstreamException()
    {
        var handler = new ControlledHandler((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new FailingHttpContent(),
            }));
        var dispatcher = CreateDispatcher(handler);

        await Assert.ThrowsAsync<EndpointUpstreamException>(() =>
            dispatcher.DispatchAsync(
                CreateEndpoint(),
                CreateRequest(),
                CancellationToken.None));
    }

    [Fact]
    public async Task WhenClientCancelsHasPreservedClientCancellation()
    {
        var handler = new ControlledHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var dispatcher = CreateDispatcher(handler);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            dispatcher.DispatchAsync(
                CreateEndpoint(),
                CreateRequest(),
                cancellationTokenSource.Token));

        Assert.IsNotType<EndpointDispatchTimeoutException>(exception);
    }

    [Fact]
    public async Task WhenTargetUrlIsInvalidHasRejectedConfigurationBeforeSending()
    {
        var handler = new ControlledHandler((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)));
        var dispatcher = CreateDispatcher(handler);
        var endpoint = new EndpointDomain(
            "Hermes",
            "/messages",
            EHttpMethods.Post,
            targetUrl: "file:///etc/passwd");

        await Assert.ThrowsAsync<EndpointConfigurationException>(() =>
            dispatcher.DispatchAsync(endpoint, CreateRequest(), CancellationToken.None));

        Assert.Equal(0, handler.CallCount);
    }

    private static EndpointDispatcher CreateDispatcher(HttpMessageHandler handler)
        => new(
            new HttpClientFactoryFake(handler),
            NullLogger<EndpointDispatcher>.Instance);

    private static EndpointDomain CreateEndpoint()
        => new(
            "Hermes",
            "/messages",
            EHttpMethods.Post,
            targetUrl: "http://hermes:8080/internal/messages?configured=true");

    private static DispatchRequest CreateRequest(
        string queryString = "",
        string body = "",
        IReadOnlyDictionary<string, IReadOnlyList<string>>? headers = null)
        => new(
            "/messages",
            HttpMethod.Post.Method,
            queryString,
            Encoding.UTF8.GetBytes(body),
            headers ?? new Dictionary<string, IReadOnlyList<string>>(),
            "correlation-123");

    private sealed class HttpClientFactoryFake(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class ControlledHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseFactory)
        : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return responseFactory(request, cancellationToken);
        }
    }

    private sealed class RequestCapture
    {
        public HttpMethod? Method { get; private set; }
        public Uri? Uri { get; private set; }
        public string Body { get; private set; } = string.Empty;
        public string? ContentType { get; private set; }
        public Dictionary<string, string[]> Headers { get; } =
            new(StringComparer.OrdinalIgnoreCase);

        public async Task CaptureAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Headers.Clear();
            Method = request.Method;
            Uri = request.RequestUri;
            Body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            ContentType = request.Content?.Headers.ContentType?.ToString();

            foreach (var header in request.Headers)
                Headers[header.Key] = header.Value.ToArray();

            if (request.Content is not null)
            {
                foreach (var header in request.Content.Headers)
                    Headers[header.Key] = header.Value.ToArray();
            }
        }
    }

    private sealed class FailingHttpContent : HttpContent
    {
        protected override Task SerializeToStreamAsync(
            Stream stream,
            TransportContext? context)
            => throw new IOException("Invalid upstream response body.");

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}
