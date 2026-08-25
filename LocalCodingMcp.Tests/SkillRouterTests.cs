using System.Reflection;
using LocalCodingMcp.Services;

namespace LocalCodingMcp.Tests;

public sealed class SkillRouterTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"local-coding-mcp-skill-router-{Guid.NewGuid():N}");

    [Fact]
    public void RoutesUiDesignToHallmark()
    {
        using var context = CreateContext("hallmark");
        var names = Route(context.Store, "Redesign the settings page UI and improve responsive UX");

        Assert.Contains("hallmark", names);
    }

    [Fact]
    public void RoutesDebuggingToSuperpowers()
    {
        using var context = CreateContext("superpowers");
        var names = Route(context.Store, "Debug this failing GitHub Actions build and find the root cause");

        Assert.Contains("superpowers", names);
    }

    [Fact]
    public void RoutesTerseCommunicationToCaveman()
    {
        using var context = CreateContext("caveman");
        var names = Route(context.Store, "Keep the coding response extremely terse with no filler");

        Assert.Contains("caveman", names);
    }

    [Fact]
    public void RoutesCodebaseExplorationToCodebaseMemory()
    {
        using var context = CreateContext("codebase-memory");
        var names = Route(context.Store, "Explore the codebase architecture and trace the call path before changing it");

        Assert.Contains("codebase-memory", names);
    }

    [Fact]
    public void CodebaseMemoryIsDisabledByDefault()
    {
        using var context = CreateContext();

        var skill = context.Store.Get("codebase-memory");

        Assert.True(skill.BuiltIn);
        Assert.False(skill.Enabled);
    }

    [Fact]
    public void DisabledSkillsAreNeverRouted()
    {
        using var context = CreateContext("hallmark");
        context.Store.SetEnabled("hallmark", false);

        var names = Route(context.Store, "Design a polished responsive landing page UI");

        Assert.DoesNotContain("hallmark", names);
    }

    [Fact]
    public void CustomSkillDescriptionParticipatesInRouting()
    {
        using var context = CreateContext();
        context.Store.Create("sql-performance", """
            ---
            name: sql-performance
            description: Diagnose SQL Server query plans, indexes, deadlocks, and slow database queries.
            ---
            # SQL performance
            """);

        var names = Route(context.Store, "Investigate this slow SQL Server query and its execution plan");

        Assert.Contains("sql-performance", names);
    }

    [Fact]
    public void UnrelatedTaskDoesNotForceSkills()
    {
        using var context = CreateContext("hallmark", "superpowers", "caveman", "ponytail", "codebase-memory");

        var names = Route(context.Store, "What is the capital of France?");

        Assert.Empty(names);
    }

    private TestContext CreateContext(params string[] enabledBuiltIns)
    {
        Directory.CreateDirectory(_root);
        var store = new SkillStore(_root);
        foreach (var name in enabledBuiltIns)
        {
            store.SetEnabled(name, true);
        }

        return new TestContext(store);
    }

    private static IReadOnlyList<string> Route(SkillStore store, string task)
    {
        var assembly = typeof(SkillStore).Assembly;
        var routerType = assembly.GetType("LocalCodingMcp.Services.SkillRouter");
        Assert.NotNull(routerType);

        var constructor = routerType!.GetConstructor([typeof(SkillStore)]);
        Assert.NotNull(constructor);
        var router = constructor!.Invoke([store]);

        var routeMethod = routerType.GetMethod("Route", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(routeMethod);
        var result = routeMethod!.Invoke(router, [task]);
        Assert.NotNull(result);

        return ((System.Collections.IEnumerable)result!)
            .Cast<object>()
            .Select(item => item.GetType().GetProperty("Name")?.GetValue(item)?.ToString())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .ToArray();
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private sealed class TestContext(SkillStore store) : IDisposable
    {
        public SkillStore Store { get; } = store;
        public void Dispose() { }
    }
}
