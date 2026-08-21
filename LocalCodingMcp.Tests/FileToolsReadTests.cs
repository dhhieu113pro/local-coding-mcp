using LocalCodingMcp.Tools;

namespace LocalCodingMcp.Tests;

public class FileToolsReadTests
{
    [Fact]
    public void TryReadAllText_Readable_ReturnsContent()
    {
        var path = Path.Combine(Path.GetTempPath(), "mcp-read-" + Guid.NewGuid().ToString("N")[..8] + ".txt");
        File.WriteAllText(path, "hello-read");
        try
        {
            var text = FileTools.TryReadAllText(path);
            Assert.Equal("hello-read", text);
        }
        finally
        {
            try { File.Delete(path); } catch { }
        }
    }

    [Fact]
    public void TryReadAllText_Missing_ReturnsNull()
    {
        var path = Path.Combine(Path.GetTempPath(), "mcp-missing-" + Guid.NewGuid().ToString("N")[..8] + ".txt");
        var text = FileTools.TryReadAllText(path);
        Assert.Null(text);
    }

    [Fact]
    public void TryReadAllText_Directory_ReturnsNull()
    {
        var path = Path.Combine(Path.GetTempPath(), "mcp-dir-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(path);
        try
        {
            var text = FileTools.TryReadAllText(path);
            Assert.Null(text);
        }
        finally
        {
            try { Directory.Delete(path); } catch { }
        }
    }
}
