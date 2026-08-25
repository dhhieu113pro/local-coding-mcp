using System.Net;
using System.Text;
using System.Text.Json;
using LocalCodingMcp.Services;
using LocalCodingMcp.Tools;
using ModelContextProtocol.Server;

namespace LocalCodingMcp.Tests;

public sealed class RemoteSkillToolTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"local-coding-mcp-remote-tools-{Guid.NewGuid():N}");

    [Fact]
    public void RemoteSkillMethods_AreExposedAsMcpTools()
    {
        foreach (var name in new[] { "InstallSkill", "CheckSkillUpdates", "UpdateSkillFromSource" })
        {
            var method = typeof(SkillTools).GetMethod(name);
            Assert.NotNull(method);
            Assert.NotNull(method!.GetCustomAttributes(typeof(McpServerToolAttribute), inherit: false).SingleOrDefault());
        }
    }

    [Fact]
    public async Task InstallCheckAndUpdate_ReturnCompactJsonContracts()
    {
        Directory.CreateDirectory(_root);
        var store = new SkillStore(_root, seedBuiltIns: false);
        var contents = new Queue<string>(new[]
        {
            "---\nname: example\ndescription: Example skill\n---\nv1",
            "---\nname: example\ndescription: Example skill\n---\nv2",
            "---\nname: example\ndescription: Example skill\n---\nv2"
        });
        using var client = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(contents.Dequeue(), Encoding.UTF8, "text/markdown")
        }));
        var remote = new RemoteSkillService(store, new RemoteSkillFetcher(client, 1024 * 1024, 3));
        var tools = new SkillTools(store, remote);

        using var installed = JsonDocument.Parse(await tools.InstallSkill("https://example.com/SKILL.md"));
        Assert.Equal("example", installed.RootElement.GetProperty("name").GetString());
        Assert.True(installed.RootElement.TryGetProperty("content_sha256", out _));
        Assert.False(installed.RootElement.TryGetProperty("content", out _));

        using var checkedDoc = JsonDocument.Parse(await tools.CheckSkillUpdates("example"));
        Assert.Equal("update_available", checkedDoc.RootElement[0].GetProperty("status").GetString());
        Assert.False(checkedDoc.RootElement[0].TryGetProperty("content", out _));

        using var updated = JsonDocument.Parse(await tools.UpdateSkillFromSource("example"));
        Assert.True(updated.RootElement.GetProperty("changed").GetBoolean());
        Assert.True(updated.RootElement.TryGetProperty("old_sha256", out _));
        Assert.True(updated.RootElement.TryGetProperty("new_sha256", out _));
    }

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
