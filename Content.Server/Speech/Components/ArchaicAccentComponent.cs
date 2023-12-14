using Content.Server.Speech.EntitySystems;

namespace Content.Server.Speech.Components;

[RegisterComponent]
[Access(typeof(ArchaicAccentSystem))]
public sealed partial class ArchaicAccentComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("forsoothChance")]
    public float ForsoothChance = 0.15f;

    [ViewVariables]
    public readonly List<string> ArchaicWords = new()
    {
        "accent-archaic-prefix-1",
        "accent-archaic-prefix-2",
        "accent-archaic-prefix-3",
        "accent-archaic-prefix-4"
    };
}
