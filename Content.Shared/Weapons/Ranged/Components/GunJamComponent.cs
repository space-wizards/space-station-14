using Content.Shared.Defects.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged.Components;

// Gives a gun a per-shot chance to jam. A jammed gun cannot fire until the player
// racks the slide (Z / Use In Hand), which instantly clears the jam.
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GunJamComponent : DefectComponent
{
    // Whether the gun is currently jammed.
    [DataField, AutoNetworkedField]
    public bool IsJammed;

    // Per-shot probability (0–1) of jamming after a successful shot.
    [DataField]
    public float JamChance = 0.05f;
}
