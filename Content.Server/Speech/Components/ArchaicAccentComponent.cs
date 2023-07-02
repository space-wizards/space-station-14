using Content.Server.Speech.EntitySystems;

namespace Content.Server.Speech.Components;

[RegisterComponent]
[Access(typeof(ArchaicAccentSystem))]
public sealed class ArchaicAccentComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("forsoothChance")]
    public readonly float ForsoothChance = 0.1f;

    [ViewVariables]
    public readonly List<string> ArchaicWords = new()
    {
        "accent-archaic-prefix-1"
    };
}
