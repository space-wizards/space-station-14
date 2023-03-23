using Content.Server.Speech.EntitySystems;

namespace Content.Server.Speech.Components;

[RegisterComponent]
[Access(typeof(PirateAccentSystem))]
public sealed class PirateAccentComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public readonly float YarrChance = 0.5f;

    [ViewVariables]
    public readonly List<string> PirateWords = new()
    {
        { "accent-pirate-prefix-1" },
        { "accent-pirate-prefix-2" },
        { "accent-pirate-prefix-3" }
    };

    [ViewVariables]
    public readonly Dictionary<string, string> DirectReplacements = new()
    {
        { "accent-pirate-replaced-1", "accent-pirate-replacement-1" },
        { "accent-pirate-replaced-2", "accent-pirate-replacement-2" },
        { "accent-pirate-replaced-3", "accent-pirate-replacement-3" },
        { "accent-pirate-replaced-4", "accent-pirate-replacement-4" },
        { "accent-pirate-replaced-5", "accent-pirate-replacement-5" },
    };
}
