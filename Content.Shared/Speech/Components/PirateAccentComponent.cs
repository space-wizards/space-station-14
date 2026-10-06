using Content.Shared.Speech.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Speech.Components;

/// <summary>
/// Lets you speak Ratvarian!
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(PirateAccentSystem))]
public sealed partial class PirateAccentComponent : BaseAccentComponent
{
    [DataField]
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
