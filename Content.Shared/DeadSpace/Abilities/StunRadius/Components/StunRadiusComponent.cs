// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;

namespace Content.Shared.DeadSpace.Abilities.StunRadius.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class StunRadiusComponent : Component
{
    [DataField]
    public EntProtoId ActionStunRadius = "ActionStunRadius";

    [DataField]
    public EntityUid? ActionStunRadiusEntity;

    [DataField, AutoNetworkedField, Access(Other = AccessPermissions.ReadWrite)]
    public float ParalyzeTime = 3f;

    [DataField, AutoNetworkedField, Access(Other = AccessPermissions.ReadWrite)]
    public float LaunchForwardsMultiplier = 1f;

    [DataField, AutoNetworkedField, Access(Other = AccessPermissions.ReadWrite)]
    public float RangeStun = 5f;

    [DataField]
    public SoundSpecifier? StunRadiusSound = default;

    [DataField]
    public bool IgnorAlien = true;

    [DataField]
    public bool StunBorg = false;
}
