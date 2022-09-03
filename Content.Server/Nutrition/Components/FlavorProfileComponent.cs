namespace Content.Server.Nutrition.Components;

[RegisterComponent]
public sealed class FlavorProfileComponent : Component
{
    /// <summary>
    ///     Localized string containing the base flavor of this entity.
    /// </summary>
    [DataField("flavors")]
    public HashSet<string> Flavors { get; } = new();

    /// <summary>
    ///     Reagent IDs to ignore when processing this flavor profile. Defaults to nutriment.
    /// </summary>
    [DataField("ignoreReagents")]
    public HashSet<string> IgnoreReagents { get; } = new()
    {
        "Nutriment",
        "Vitamin",
        "Protein"
    };
}
