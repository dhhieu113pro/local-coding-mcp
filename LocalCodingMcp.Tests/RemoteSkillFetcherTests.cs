using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using LocalCodingMcp.Services;

namespace LocalCodingMcp.Tests;

public sealed class RemoteSkillFetcherTests
{
    [Fact]
    public async Task FetchAsync_NormalizesGithubBlobAndCapturesMetadata()
    {
        var handler = new StubHandler(request =>
        {
            Assert.Equal("https://raw.githubusercontent.com/owner/repo/main/path/SKILL.md", request.RequestUri!.ToString());
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("---\nname: x\ndescription: y\n---\nbody", Encoding.UTF8, "text/markdown")
            };
            response.Headers.ETag = new EntityTagHeaderValue("\"abc\"");
            response.Content.Headers.LastModified = new DateTimeOffset(2026, 8, 25, 0, 0, 0, TimeSpan.Zero);
            return response;
        });
        using var client = new HttpClient(handler);
        var fetcher = new RemoteSkillFetcher(client, 1024 * 1024, 3);

        var result = await fetcher.FetchAsync("https://github.com/owner/repo/blob/main/path/SKILL.md");

        Assert.Equal("https://github.com/owner/repo/blob/main/path/SKILL.md", result.SourceUrl);
        Assert.Equal("https://raw.githubusercontent.com/owner/repo/main/path/SKILL.md", result.ResolvedSourceUrl);
        Assert.Equal("\"abc\"", result.ETag);
        Assert.NotNull(result.LastModified);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(result.Content))).ToLowerInvariant(), result.ContentSha256);
    }

    [Theory]
    [InlineData("http://example.com/SKILL.md")]
    [InlineData("https://user:pass@example.com/SKILL.md")]
    public async Task FetchAsync_RejectsUnsafeSource(string source)
    {
        using var client = new HttpClient(new StubHandler(_ => throw new InvalidOperationException("should not send")));
        var fetcher = new RemoteSkillFetcher(client, 1024, 3);
        await Assert.ThrowsAsync<InvalidDataException>(() => fetcher.FetchAsync(source));
    }

    [Fact]
    public async Task FetchAsync_RejectsHttpsToHttpRedirect()
    {
        using var client = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Redirect)
        {
            Headers = { Location = new Uri("http://example.com/SKILL.md") }
        }));
        var fetcher = new RemoteSkillFetcher(client, 1024, 3);
        await Assert.ThrowsAsync<InvalidDataException>(() => fetcher.FetchAsync("https://example.com/SKILL.md"));
    }

    [Fact]
    public async Task FetchAsync_RejectsOversizedBinaryAndEmptyBodies()
    {
        var responses = new Queue<HttpResponseMessage>(new[]
        {
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[20]) },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([0, 1, 2]) },
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("", Encoding.UTF8, "text/plain") }
        });
        responses.ElementAt(0).Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        responses.ElementAt(1).Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        using var client = new HttpClient(new StubHandler(_ => responses.Dequeue()));
        var fetcher = new RemoteSkillFetcher(client, 10, 3);

        await Assert.ThrowsAsync<InvalidDataException>(() => fetcher.FetchAsync("https://example.com/large"));
        await Assert.ThrowsAsync<InvalidDataException>(() => fetcher.FetchAsync("https://example.com/binary"));
        await Assert.ThrowsAsync<InvalidDataException>(() => fetcher.FetchAsync("https://example.com/empty"));
    }

    [Fact]
    public async Task FetchAsync_SendsConditionalHeaders()
    {
        var lastModified = new DateTimeOffset(2026, 8, 24, 0, 0, 0, TimeSpan.Zero);
        using var client = new HttpClient(new StubHandler(request =>
        {
            Assert.Contains(request.Headers.IfNoneMatch, h => h.Tag == "\"abc\"");
            Assert.Equal(lastModified, request.Headers.IfModifiedSince);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("---\nname: x\ndescription: y\n---\nbody", Encoding.UTF8, "text/plain")
            };
        }));
        var fetcher = new RemoteSkillFetcher(client, 1024, 3);
        await fetcher.FetchAsync("https://example.com/SKILL.md", "\"abc\"", lastModified);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }
}
