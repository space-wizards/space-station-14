using System.Text.Json.Serialization;
using Robust.Shared.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Kitchen.Components;

namespace Content.Server.GuideGenerator;

public sealed class GrindRecipeEntry
{
    /// <summary>
    ///     Id of grindable item
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; }

    /// <summary>
    ///     Human-readable name of recipe.
    ///     Should automatically be localized by default
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; }

    /// <summary>
    ///     Type of recipe
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; }

    /// <summary>
    ///     Item that will be grinded into something
    /// </summary>
    [JsonPropertyName("input")]
    public string Input { get; }

    /// <summary>
    ///     Dictionary of reagents that entity contains; aka "Recipe Result"
    /// </summary>
    [JsonPropertyName("result")]
    public Dictionary<string, int>? Result { get; } = new Dictionary<string, int>();


    public GrindRecipeEntry(EntityPrototype proto)
    {
        Id = proto.ID;
        Name = TextTools.TextTools.CapitalizeString(proto.Name);
        Type = "grindableRecipes";
        Input = proto.ID;
        var foodSolutionName = "food"; // default to food because everything in prototypes defaults to "food"

        // Now, to become a recipe, entity must:
        // A) Have "Extractable" component on it.
        // B) Have "SolutionContainerManager" component on it.
        // C) Have "GrindableSolution" declared in "SolutionContainerManager" component.
        // D) Have solution with name declared in "SolutionContainerManager.GrindableSolution" inside its "SolutionContainerManager" component.
        // F) Have "Food" in its name (see Content.Server/Corvax/GuideGenerator/MealsRecipesJsonGenerator.cs)
        if (proto.Components.TryGetComponent("Extractable", out var extractableComp) && proto.Components.TryGetComponent("SolutionContainerManager", out var solutionCompRaw))
        {
            var extractable = (ExtractableComponent) extractableComp;
            var solutionComp = (SolutionContainerManagerComponent) solutionCompRaw;
            foodSolutionName = extractable.GrindableSolution;

            if (solutionComp.Solutions != null && foodSolutionName != null)
            {
                foreach (ReagentQuantity reagent in solutionComp.Solutions[(string) foodSolutionName].Contents)
                {
                    Result[reagent.Reagent.Prototype] = reagent.Quantity.Int();
                }
            }
            else
                Result = null;
        }
    }
}
