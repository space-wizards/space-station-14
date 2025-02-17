using System.Text.Json.Serialization;
using Robust.Shared.Prototypes;
using Content.Server.Nutrition.Components;

namespace Content.Server.GuideGenerator;

public sealed class SliceRecipeEntry
{
    /// <summary>
    ///     Id of sliceable item
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
    ///     Item that will be sliced into something
    /// </summary>
    [JsonPropertyName("input")]
    public string Input { get; }

    /// <summary>
    ///     Result of a recipe
    /// </summary>
    [JsonPropertyName("result")]
    public string Result { get; }

    /// <summary>
    ///     Count of result item
    /// </summary>
    [JsonPropertyName("count")]
    public int Count { get; }


    public SliceRecipeEntry(EntityPrototype proto)
    {
        Id = proto.ID;
        Name = TextTools.TextTools.CapitalizeString(proto.Name);
        Type = "sliceableRecipes";
        Input = proto.ID;
        if (proto.Components.TryGetComponent("SliceableFood", out var comp))
        {
            var sliceable = (SliceableFoodComponent) comp;
            Result = sliceable.Slice ?? "";
            Count = sliceable.TotalCount;
        }
        else // just in case something will go wrong and we somehow will not get our component
        {
            Result = "";
            Count = 0;
        }
    }
}
