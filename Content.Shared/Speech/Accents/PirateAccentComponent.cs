namespace Content.Shared.Speech.Accents;

/// <summary>
/// Yarr... Shiver me timbers.
/// </summary>
[RegisterComponent]
public sealed partial class PirateAccentComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("yarrChance")]
    public float YarrChance = 0.5f;

    [ViewVariables]
    public readonly List<string> PirateWords = new()
    {
        "accent-pirate-prefix-1",
        "accent-pirate-prefix-2",
        "accent-pirate-prefix-3",
        "accent-pirate-prefix-4",
    };
}
