using Loom;
using Loom.Sample.Handlers;

var recipePath = Path.Combine(AppContext.BaseDirectory, "Recipes", "initial-setup.json");

var engine = RecipeEngine.Create()
    .RegisterHandler(new CaptureStepHandler())
    .RegisterHandler(new PrintStepHandler())
    .AddSource(new FileRecipeSource(recipePath, new JsonRecipeSerializer(), "sample-file"));

var catalog = await engine.DiscoverAsync();
foreach (var diagnostic in catalog.Diagnostics)
{
    Console.WriteLine($"{diagnostic.Severity} {diagnostic.Code}: {diagnostic.Message}");
}

var recipe = catalog.Recipes.Single();
var result = await engine.RunAsync(recipe);

Console.WriteLine($"Status: {result.Status}");
Console.WriteLine($"Completed steps: {result.CompletedSteps.Count}");
