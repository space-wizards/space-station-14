using Content.Shared.Thief.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Thief.Components;

/// <summary>
/// working together with StealAreaComponent, allows the thief to count objects near the beacon as stolen when setting up.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ThiefBeaconSystem))]
public sealed partial class ThiefBeaconComponent : Component
{
    [DataField]
    public SoundSpecifier LinkSound = new SoundPathSpecifier("/Audio/Machines/high_tech_confirm.ogg");

    [DataField]
    public SoundSpecifier UnlinkSound = new SoundPathSpecifier("/Audio/Machines/beep.ogg");
}
