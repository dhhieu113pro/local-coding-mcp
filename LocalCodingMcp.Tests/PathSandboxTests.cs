using LocalCodingMcp.Services;

namespace LocalCodingMcp.Tests;

public class PathSandboxTests : IDisposable
{
    private readonly string _root;
    private readonly PathSandbox _sandbox;

    public PathSandboxTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "mcp-test-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_root);
        Directory.CreateDirectory(Path.Combine(_root, "sub"));
        File.WriteAllText(Path.Combine(_root, "sub", "a.txt"), "hello");
        _sandbox = new PathSandbox(new[] { _root });
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, true); } catch { }
    }

    [Fact]
    public void AllowedRoots_ContainsConfiguredRoot()
    {
        Assert.NotEmpty(_sandbox.AllowedRoots);
        Assert.True(_sandbox.IsUnderAllowedRoot(_root));
    }

    [Fact]
    public void Resolve_RelativePath_Succeeds()
    {
        var full = _sandbox.Resolve(_root, "sub/a.txt");
        Assert.True(File.Exists(full));
        Assert.EndsWith("a.txt", full);
    }

    [Fact]
    public void Resolve_DotPath_Succeeds()
    {
        var full = _sandbox.Resolve(_root, ".");
        Assert.True(_sandbox.IsUnderAllowedRoot(full));
    }

    [Fact]
    public void Resolve_AbsoluteInside_Succeeds()
    {
        var target = Path.Combine(_root, "sub", "a.txt");
        var full = _sandbox.Resolve(_root, target);
        Assert.True(File.Exists(full));
    }

    [Fact]
    public void Resolve_NonExistingUnderParent_Succeeds()
    {
        // file does not exist yet – still must resolve under sandbox
        var full = _sandbox.Resolve(_root, "sub/newfile.txt");
        Assert.True(_sandbox.IsUnderAllowedRoot(full));
        Assert.EndsWith("newfile.txt", full);
    }

    [Fact]
    public void Resolve_Traversal_Throws()
    {
        Assert.Throws<UnauthorizedAccessException>(() =>
            _sandbox.Resolve(_root, "../outside.txt"));
    }

    [Fact]
    public void Resolve_AbsoluteOutside_Throws()
    {
        Assert.Throws<UnauthorizedAccessException>(() =>
            _sandbox.Resolve(_root, "/etc/passwd"));
    }

    [Fact]
    public void Resolve_InvalidChars_Throws()
    {
        var bad = "file\0name.txt";
        Assert.ThrowsAny<Exception>(() => _sandbox.Resolve(_root, bad));
    }

    [Fact]
    public void RequireInsideAllowedRoots_Valid_Succeeds()
    {
        var full = _sandbox.RequireInsideAllowedRoots(_root);
        Assert.False(string.IsNullOrEmpty(full));
    }

    [Fact]
    public void RequireInsideAllowedRoots_Invalid_Throws()
    {
        var outside = Path.Combine(Path.GetTempPath(), "mcp-outside-" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(outside);
        try
        {
            Assert.Throws<UnauthorizedAccessException>(() =>
                _sandbox.RequireInsideAllowedRoots(outside));
        }
        finally
        {
            try { Directory.Delete(outside, true); } catch { }
        }
    }

    [Fact]
    public void RequireInsideAllowedRoots_Empty_Throws()
    {
        Assert.Throws<ArgumentException>(() => _sandbox.RequireInsideAllowedRoots("  "));
    }

    [Fact]
    public void RequireInsideAllowedRoots_InvalidChars_Throws()
    {
        Assert.ThrowsAny<Exception>(() =>
            _sandbox.RequireInsideAllowedRoots("/tmp/bad\0path"));
    }

    [Fact]
    public void IsUnderAllowedRoot_TrueForChild()
    {
        var child = Path.Combine(_root, "sub", "a.txt");
        Assert.True(_sandbox.IsUnderAllowedRoot(Path.GetFullPath(child)));
    }

    [Fact]
    public void IsUnderAllowedRoot_FalseForSiblingPrefix()
    {
        var evil = _root + "-evil";
        Assert.False(_sandbox.IsUnderAllowedRoot(evil));
    }

    [Fact]
    public void IsUnderAllowedRoot_Empty_False()
    {
        Assert.False(_sandbox.IsUnderAllowedRoot(""));
        Assert.False(_sandbox.IsUnderAllowedRoot("   "));
    }

    [Fact]
    public void Constructor_EmptyRoots_Throws()
    {
        Assert.Throws<ArgumentException>(() => new PathSandbox(Array.Empty<string>()));
    }

    [Fact]
    public void Constructor_NullRoots_Throws()
    {
        Assert.Throws<ArgumentException>(() => new PathSandbox(null!));
    }

    [Fact]
    public void Constructor_BlankRoot_Throws()
    {
        Assert.ThrowsAny<Exception>(() => new PathSandbox(new[] { "  " }));
    }

    [Fact]
    public void Resolve_EmptyPath_Throws()
    {
        Assert.Throws<ArgumentException>(() => _sandbox.Resolve(_root, "  "));
    }

    [Fact]
    public void ResolveSymbolicLinks_ExistingFile()
    {
        var path = Path.Combine(_root, "sub", "a.txt");
        var resolved = PathSandbox.ResolveSymbolicLinks(path);
        Assert.False(string.IsNullOrEmpty(resolved));
    }

    [Fact]
    public void ResolveSymbolicLinks_NonExistingWithParent()
    {
        var path = Path.Combine(_root, "sub", "does-not-exist-yet.txt");
        var resolved = PathSandbox.ResolveSymbolicLinks(path);
        Assert.Contains("does-not-exist-yet.txt", resolved);
    }

    [Fact]
    public void ResolveSymbolicLinks_MissingParent_ReturnsOriginal()
    {
        var path = Path.Combine(_root, "no-such-dir", "x.txt");
        var resolved = PathSandbox.ResolveSymbolicLinks(path);
        Assert.False(string.IsNullOrEmpty(resolved));
    }

    [Fact]
    public void Symlink_InsideRoot_Resolves()
    {
        if (OperatingSystem.IsWindows()) return;

        var target = Path.Combine(_root, "sub", "a.txt");
        var link = Path.Combine(_root, "link-a.txt");
        try
        {
            File.CreateSymbolicLink(link, target);
        }
        catch
        {
            // environment may disallow symlinks
            return;
        }

        var full = _sandbox.Resolve(_root, "link-a.txt");
        Assert.True(_sandbox.IsUnderAllowedRoot(full));
    }

    [Fact]
    public void Symlink_Escape_Throws()
    {
        if (OperatingSystem.IsWindows()) return;

        var outside = Path.Combine(Path.GetTempPath(), "mcp-escape-" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(outside);
        File.WriteAllText(Path.Combine(outside, "secret.txt"), "x");
        var link = Path.Combine(_root, "escape-link");
        try
        {
            try
            {
                Directory.CreateSymbolicLink(link, outside);
            }
            catch
            {
                return;
            }

            // Resolving through symlink that points outside should fail
            Assert.ThrowsAny<Exception>(() =>
                _sandbox.Resolve(_root, "escape-link/secret.txt"));
        }
        finally
        {
            try { if (Directory.Exists(link) || File.Exists(link)) File.Delete(link); } catch { }
            try { Directory.Delete(link, true); } catch { }
            try { Directory.Delete(outside, true); } catch { }
        }
    }

    [Fact]
    public void MultipleRoots_Work()
    {
        var root2 = Path.Combine(Path.GetTempPath(), "mcp-test2-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(root2);
        try
        {
            var sandbox = new PathSandbox(new[] { _root, root2 });
            Assert.True(sandbox.IsUnderAllowedRoot(_root));
            Assert.True(sandbox.IsUnderAllowedRoot(root2));
            var f = sandbox.Resolve(root2, "x.txt");
            Assert.True(sandbox.IsUnderAllowedRoot(f));
        }
        finally
        {
            try { Directory.Delete(root2, true); } catch { }
        }
    }
}

public class PathSandboxResolveExtras
{
    [Fact]
    public void ResolveSymbolicLinks_Empty_ReturnsEmpty()
    {
        Assert.Equal("", PathSandbox.ResolveSymbolicLinks(""));
        Assert.Equal("   ", PathSandbox.ResolveSymbolicLinks("   "));
    }

    [Fact]
    public void ResolveSymbolicLinks_RootOnly()
    {
        var r = PathSandbox.ResolveSymbolicLinks("/");
        Assert.False(string.IsNullOrEmpty(r));
    }
}

public class PathSandboxFinalCoverage
{
    [Fact]
    public void IsUnderAllowedRoot_InvalidPath_ReturnsFalse()
    {
        var root = Path.Combine(Path.GetTempPath(), "mcp-fc-" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(root);
        try
        {
            var sandbox = new PathSandbox(new[] { root });
            // null char should make NormalizeForComparison throw → false
            Assert.False(sandbox.IsUnderAllowedRoot("bad\0path"));
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }
}
