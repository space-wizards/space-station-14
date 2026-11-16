using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Hitscan.Components;

/// <summary>
/// System or basic "effects" like sounds and hit markers for hitscans.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HitscanBasicEffectsComponent : Component
{
    /// <summary>
    /// This will turn hit entities this color briefly.
    /// </summary>
    [DataField]
    public Color? HitColor = Color.Red;

    /// <summary>
    /// Sound that plays upon the thing being hit.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound;

    /// <summary>
    /// Force the hitscan sound to play rather than playing the entity's override sound (if it exists).
    /// </summary>
    [DataField]
    public bool ForceSound;
}
