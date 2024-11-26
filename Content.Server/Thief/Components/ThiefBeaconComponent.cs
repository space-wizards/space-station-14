using Content.Server.Thief.Systems;
using Robust.Shared.Audio;

namespace Content.Server.Thief.Components;

/// <summary>
/// working together with StealAreaComponent, allows the thief to count objects near the beacon as stolen when setting up.
/// </summary>
[RegisterComponent, Access(typeof(ThiefBeaconSystem))]
public sealed partial class ThiefBeaconComponent : Component
{
    [DataField]
    public SoundSpecifier LinkSound = new SoundCollectionSpecifier("ThiefBeaconLinkSound");

    [DataField]
    public SoundSpecifier UnlinkSound = new SoundCollectionSpecifier("ThiefBeaconUnlinkSound");
}
