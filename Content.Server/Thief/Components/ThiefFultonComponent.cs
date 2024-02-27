using Content.Server.Thief.Systems;
using Robust.Shared.Audio;

namespace Content.Server.Thief.Components;

/// <summary>
/// counts targets for stealing nearby objects from a target that is connected to this beacon.
/// </summary>
[RegisterComponent, Access(typeof(ThiefFultonSystem))]
public sealed partial class ThiefFultonComponent : Component
{
    [DataField]
    public EntityUid? LinkedOwner;

    [DataField]
    public float ThievingRange = 1f;

    [DataField]
    public SoundSpecifier LinkSound = new SoundPathSpecifier("/Audio/Machines/high_tech_confirm.ogg");

    [DataField]
    public SoundSpecifier UnlinkSound = new SoundPathSpecifier("/Audio/Machines/beep.ogg");
}
