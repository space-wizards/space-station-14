using Content.Server.Speech.EntitySystems;

namespace Content.Server.Speech.Components;

[RegisterComponent]
[Access(typeof(PirateAccentSystem))]
public sealed class EarlyEnglishAccentComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("forsoothChance")]
    public readonly float ForsoothChance = 0.5f;

    [ViewVariables]
    public readonly List<string> EarlyEnglishWords = new()
    {
        "accent-earlyenglish-prefix-1"
    };
}
