using Content.Server.Thief.Systems;
using Content.Shared.Thief;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Thief.Components;

/// <summary>
/// 
/// </summary>
[RegisterComponent, Access(typeof(ThiefFultonSystem))]
public sealed partial class ThiefFultonComponent : Component
{
    [DataField]
    public EntityUid? LinkedOwner;

    [DataField]
    public float ThievingRange = 2f;

    [DataField]
    public SoundSpecifier AccessDeniedSound = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");

    [DataField]
    public SoundSpecifier LinkSound = new SoundPathSpecifier("/Audio/Machines/beep.ogg");
}
