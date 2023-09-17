using Content.Server.Speech.EntitySystems;

namespace Content.Server.Speech.Components;

[RegisterComponent]
[Access(typeof(LoudSystem))]
public sealed partial class LoudAccentComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("yellChance")]
    public float YellChance = 0.8f;

    [ViewVariables]
    public readonly List<string> YellSuffixes = new()
    {
        "accent-loud-suffix-1",
        "accent-loud-suffix-2",
        "accent-loud-suffix-3",
        "accent-loud-suffix-4"
    };
}
