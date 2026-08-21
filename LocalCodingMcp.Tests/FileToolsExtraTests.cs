using LocalCodingMcp.Services;
using LocalCodingMcp.Tools;

namespace LocalCodingMcp.Tests;

public class FileToolsExtraTests : IDisposable
{
    private readonly string _root;
    private readonly FileTools _tools;
    private readonly string _workspaceId;

    public FileToolsExtraTests()
    {
        var (root, sandbox, ws, filter, _) = TestHelpers.CreateEnv();
        _root = root;
        File.WriteAllText(Path.Combine(_root, "ok.txt"), "findme-token");
        File.WriteAllBytes(Path.Combine(_root, "blob.exe"), new byte[] { 0x00, 0x01, 0x02 });
        var config = TestHelpers.Config(50);
        _tools = new FileTools(ws, sandbox, filter, config);
        _workspaceId = ws.Open(_root).Id;
    }

    public void Dispose() => TestHelpers.SafeDelete(_root);

    [Fact]
    public void SearchFiles_SkipsBinaryExtensions()
    {
        var json = _tools.SearchFiles("findme-token", _workspaceId);
        Assert.Contains("ok.txt", json);
        Assert.DoesNotContain("blob.exe", json);
    }

    [Fact]
    public void SearchFiles_UnderSubpath()
    {
        Directory.CreateDirectory(Path.Combine(_root, "src"));
        File.WriteAllText(Path.Combine(_root, "src", "x.cs"), "unique-xyz");
        var json = _tools.SearchFiles("unique-xyz", _workspaceId, path: "src");
        Assert.Contains("unique-xyz", json);
    }

    [Fact]
    public void DeleteFile_EmptyDir()
    {
        Directory.CreateDirectory(Path.Combine(_root, "emptydir"));
        var result = _tools.DeleteFile("emptydir", _workspaceId);
        Assert.Contains("Deleted", result);
    }

    [Fact]
    public void MoveFile_Directory()
    {
        Directory.CreateDirectory(Path.Combine(_root, "d1"));
        File.WriteAllText(Path.Combine(_root, "d1", "f.txt"), "z");
        _tools.MoveFile("d1", "d2", _workspaceId);
        Assert.True(Directory.Exists(Path.Combine(_root, "d2")));
    }

    [Fact]
    public void WriteBinaryFile_And_ReadBinaryFile_RoundTrip()
    {
        // Minimal PNG header-ish bytes (not a valid image, fine for round-trip)
        var original = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x01, 0x02, 0x03 };
        var b64 = Convert.ToBase64String(original);

        var writeResult = _tools.WriteBinaryFile("images/test.png", b64, _workspaceId);
        Assert.Contains("Wrote binary", writeResult);
        Assert.True(File.Exists(Path.Combine(_root, "images", "test.png")));

        var readB64 = _tools.ReadBinaryFile("images/test.png", _workspaceId);
        var roundTrip = Convert.FromBase64String(readB64);
        Assert.Equal(original, roundTrip);
    }

    [Fact]
    public void WriteBinaryFile_AcceptsDataUrlPrefix()
    {
        var bytes = new byte[] { 1, 2, 3, 4, 5 };
        var dataUrl = "data:image/png;base64," + Convert.ToBase64String(bytes);
        _tools.WriteBinaryFile("pic.png", dataUrl, _workspaceId);
        Assert.Equal(bytes, File.ReadAllBytes(Path.Combine(_root, "pic.png")));
    }

    [Fact]
    public void WriteBinaryFile_RejectsEmpty()
    {
        Assert.ThrowsAny<Exception>(() =>
            _tools.WriteBinaryFile("empty.bin", "", _workspaceId));
    }
}
