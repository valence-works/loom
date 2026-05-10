namespace Loom.Tests.Interpolation;

public sealed class RecipeInterpolationDirectiveParserTests
{
    [Fact]
    public void Parse_returns_valid_directives()
    {
        var directives = RecipeInterpolationDirectiveParser.Parse("[js: variables('tenant')]");

        var directive = Assert.Single(directives);
        Assert.Equal("js", directive.Prefix);
        Assert.Equal("variables('tenant')", directive.Expression);
    }

    [Fact]
    public void Parse_ignores_unknown_looking_text_without_valid_envelope()
    {
        var directives = RecipeInterpolationDirectiveParser.Parse("not [1js: value] and not [js value]");

        Assert.Empty(directives);
    }

    [Fact]
    public void Parse_allows_brackets_inside_expression()
    {
        var directives = RecipeInterpolationDirectiveParser.Parse("[js: variables('cfg')['key'][0]]");

        var directive = Assert.Single(directives);
        Assert.Equal("variables('cfg')['key'][0]", directive.Expression);
    }
}
