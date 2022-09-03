namespace Content.Server.Nutrition.Components;

[RegisterComponent]
public sealed class FlavorProfileComponent : Component
{
    /// <summary>
    ///     Localized string containing the base flavor of this entity.
    /// </summary>
    [DataField("flavor")] public string Flavor { get; } = default!;

    /// <summary>
    ///     Reagent IDs to ignore when processing this flavor profile. Defaults to nutriment.
    /// </summary>
    [DataField("ignoreReagents")]
    public HashSet<string> IgnoreReagents { get; } = new()
    {
        "Nutriment"
    };
}
