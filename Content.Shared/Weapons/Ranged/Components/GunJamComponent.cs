using Content.Shared.Defects.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged.Components;

// Gives a gun a per-shot chance to jam. A jammed gun cannot fire until the player
// racks the slide (Z / Use In Hand), which instantly clears the jam.
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GunJamDefectComponent : DefectComponent
{
    public GunJamDefectComponent()
    {
        DefectLabel = "damaged bolt";
    }

    // Whether the gun is currently jammed.
    [DataField, AutoNetworkedField]
    public bool IsJammed;

    // Per-shot probability (0-1) of jamming after a successful shot.
    [DataField]
    public float JamChance = 0.05f;

    // Sound to play when the gun jams (simulates the action locking up).
    [DataField]
    public SoundSpecifier SoundJamRack = new SoundPathSpecifier("/Audio/Weapons/Guns/Cock/smg_cock.ogg");

    // Minimum time between "gun is jammed" popup messages to prevent spam.
    [DataField]
    public TimeSpan PopupCooldown = TimeSpan.FromSeconds(1.0);

    // Runtime only — not networked. Tracks when the next popup is allowed.
    public TimeSpan NextPopupTime;
}
