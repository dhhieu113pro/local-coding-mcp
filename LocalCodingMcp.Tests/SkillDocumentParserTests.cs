using LocalCodingMcp.Services;

namespace LocalCodingMcp.Tests;

public sealed class SkillDocumentParserTests
{
    [Fact]
    public void Parse_ValidFrontMatter_ReturnsMetadata()
    {
        var parsed = SkillDocumentParser.Parse("""
---
name: hallmark
description: UI design discipline
license: MIT
---
# Hallmark
""");

        Assert.Equal("hallmark", parsed.Name);
        Assert.Equal("UI design discipline", parsed.Description);
        Assert.Equal("MIT", parsed.License);
    }

    [Theory]
    [InlineData("# no front matter")]
    [InlineData("---\ndescription: x\n---\nbody")]
    [InlineData("---\nname: x\n---\nbody")]
    [InlineData("---\nname:   \ndescription: x\n---\nbody")]
    [InlineData("---\nname: x\ndescription:   \n---\nbody")]
    [InlineData("---\nname: x\ndescription: x")]
    public void Parse_InvalidFrontMatter_Throws(string content)
    {
        Assert.Throws<InvalidDataException>(() => SkillDocumentParser.Parse(content));
    }
}
