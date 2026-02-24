using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// Gives a gun a per-shot chance to jam. A jammed gun cannot fire until the player
/// racks the slide (Z / Use In Hand), which instantly clears the jam.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GunJamComponent : Component
{
    /// <summary>
    /// Whether the gun is currently jammed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsJammed;

    /// <summary>
    /// Probability (0–1) of jamming after each successful shot.
    /// </summary>
    [DataField]
    public float JamChance = 0.05f;
}
