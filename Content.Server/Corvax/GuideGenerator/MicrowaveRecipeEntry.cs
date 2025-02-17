using System.Linq;
using System.Text.Json.Serialization;
using Content.Shared.Kitchen;

namespace Content.Server.GuideGenerator;

public sealed class MicrowaveRecipeEntry
{
    /// <summary>
    ///     Id of recipe
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
    ///     Time to cook something (for microwave recipes)
    /// </summary>
    [JsonPropertyName("time")]
    public uint Time { get; }

    /// <summary>
    ///     Solids required to cook something
    /// </summary>
    [JsonPropertyName("solids")]
    public Dictionary<string, uint> Solids { get; }

    /// <summary>
    ///     Reagents required to cook something
    /// </summary>
    [JsonPropertyName("reagents")]
    public Dictionary<string, uint> Reagents { get; }

    /// <summary>
    ///     Result of a recipe
    /// </summary>
    [JsonPropertyName("result")]
    public string Result { get; }


    public MicrowaveRecipeEntry(FoodRecipePrototype proto)
    {
        Id = proto.ID;
        Name = TextTools.TextTools.CapitalizeString(proto.Name);
        Type = "microwaveRecipes";
        Time = proto.CookTime;
        Solids = proto.IngredientsSolids
        .ToDictionary(
            sol => sol.Key,
            sol => (uint)(int)sol.Value.Int()
        );
        Reagents = proto.IngredientsReagents
        .ToDictionary(
            rea => rea.Key,
            rea => (uint)(int)rea.Value.Int()
        );
        Result = proto.Result;
    }
}
