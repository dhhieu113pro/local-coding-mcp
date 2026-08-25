namespace LocalCodingMcp.Tests.Hosting;

public sealed class McpServerRegistrationTests
{
    [Fact]
    public void Shared_mcp_registration_type_is_available_for_both_hosts()
    {
        var assembly = typeof(LocalCodingMcp.Tools.WorkspaceTools).Assembly;

        var registrationType = assembly.GetType("LocalCodingMcp.Hosting.McpServerRegistration");

        Assert.NotNull(registrationType);
        Assert.NotNull(registrationType!.GetMethod("AddLocalCodingMcp", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static));
    }
}
