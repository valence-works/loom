using System.Text.Json.Nodes;

namespace Loom.Tests;

internal static class RecipeBuilder
{
    public static Recipe TwoStepRecipe(JsonNode? secondInput = null)
    {
        return new Recipe(
            "setup",
            [
                new RecipeStep("record", "first", JsonNode.Parse("""{"name":"first"}""")),
                new RecipeStep("record", "second", secondInput ?? JsonNode.Parse("""{"name":"second"}"""), ["first"])
            ],
            Variables: new Dictionary<string, JsonNode?>
            {
                ["tenant"] = JsonValue.Create("acme")
            });
    }

    public static Recipe SingleStep(string type = "record", string? id = "step")
    {
        return new Recipe("single", [new RecipeStep(type, id)]);
    }
}
