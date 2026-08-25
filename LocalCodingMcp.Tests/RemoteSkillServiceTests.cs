using System.Net;
using System.Text;
using LocalCodingMcp.Services;

namespace LocalCodingMcp.Tests;

public sealed class RemoteSkillServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"local-coding-mcp-remote-{Guid.NewGuid():N}");

    [Fact]
    public async Task InstallAsync_PersistsProvenanceAndEnabledState()
    {
        var service = CreateService(_ => SkillResponse("---\nname: example\ndescription: Example skill\nlicense: MIT\n---\n# Example"));

        var skill = await service.InstallAsync("https://example.com/SKILL.md", enabled: false);
        var reloaded = new SkillStore(_root, seedBuiltIns: false).Get("example");

        Assert.False(skill.Enabled);
        Assert.False(reloaded.Enabled);
        Assert.Equal("https://example.com/SKILL.md", reloaded.SourceUrl);
        Assert.False(string.IsNullOrWhiteSpace(reloaded.ContentSha256));
        Assert.Equal("MIT", reloaded.License);
    }

    [Fact]
    public async Task InstallAsync_RejectsExpectedNameMismatch()
    {
        var service = CreateService(_ => SkillResponse("---\nname: actual\ndescription: Example skill\n---\nbody"));
        await Assert.ThrowsAsync<InvalidDataException>(() => service.InstallAsync("https://example.com/SKILL.md", name: "expected"));
    }

    [Fact]
    public async Task CheckUpdatesAsync_ReturnsChangedAndUnavailableWithoutAborting()
    {
        var installResponses = new Queue<string>(new[]
        {
            "---\nname: one\ndescription: One skill\n---\none",
            "---\nname: two\ndescription: Two skill\n---\ntwo"
        });
        var installer = CreateService(_ => SkillResponse(installResponses.Dequeue()));
        await installer.InstallAsync("https://example.com/one.md");
        await installer.InstallAsync("https://example.com/two.md");

        var service = CreateService(request => request.RequestUri!.AbsolutePath.Contains("one", StringComparison.Ordinal)
            ? SkillResponse("---\nname: one\ndescription: One skill\n---\nchanged")
            : throw new HttpRequestException("offline"));

        var statuses = await service.CheckUpdatesAsync();

        Assert.Contains(statuses, x => x.Name == "one" && x.Status == "update_available");
        Assert.Contains(statuses, x => x.Name == "two" && x.Status == "unavailable");
    }

    [Fact]
    public async Task UpdateFromSourceAsync_PreservesEnabledAndRejectsInvalidUpstream()
    {
        var installer = CreateService(_ => SkillResponse("---\nname: example\ndescription: Example skill\n---\nold"));
        await installer.InstallAsync("https://example.com/SKILL.md", enabled: false);
        var before = new SkillStore(_root, seedBuiltIns: false).Get("example");

        var updater = CreateService(_ => SkillResponse("---\nname: example\ndescription: Example skill\n---\nnew"));
        var result = await updater.UpdateFromSourceAsync("example");
        Assert.True(result.Changed);
        Assert.False(result.Skill.Enabled);

        var invalid = CreateService(_ => SkillResponse("not front matter"));
        await Assert.ThrowsAsync<InvalidDataException>(() => invalid.UpdateFromSourceAsync("example"));
        var after = new SkillStore(_root, seedBuiltIns: false).Get("example");
        Assert.Equal(result.Skill.Content, after.Content);
        Assert.NotEqual(before.ContentSha256, after.ContentSha256);
    }

    private RemoteSkillService CreateService(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        Directory.CreateDirectory(_root);
        var store = new SkillStore(_root, seedBuiltIns: false);
        var client = new HttpClient(new StubHandler(responder));
        return new RemoteSkillService(store, new RemoteSkillFetcher(client, 1024 * 1024, 3));
    }

    private static HttpResponseMessage SkillResponse(string content) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(content, Encoding.UTF8, "text/markdown")
    };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}
