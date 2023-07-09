using Content.Server.Speech.EntitySystems;

namespace Content.Server.Speech.Components;

[RegisterComponent]
[Access(typeof(PirateAccentSystem))]
public sealed class PirateAccentComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("yarrChance")]
    public readonly float YarrChance = 0.5f;

    [ViewVariables]
    public readonly List<string> PirateWords = new()
    {
        "accent-pirate-prefix-1",
        "accent-pirate-prefix-2",
        "accent-pirate-prefix-3"
    };
}
