// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Audio;

namespace Content.Shared.DeadSpace.Abilities.StunRadius.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class StunRadiusComponent : Component
{
    [DataField("actionStunRadius", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionStunRadius = "ActionStunRadius";

    [DataField("actionStunRadiusEntity")]
    public EntityUid? ActionStunRadiusEntity;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("paralyzeTime"), AutoNetworkedField]
    [Access(Other = AccessPermissions.ReadWrite)]
    public float ParalyzeTime = 3f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("launchForwardsMultiplier"), AutoNetworkedField]
    [Access(Other = AccessPermissions.ReadWrite)]
    public float LaunchForwardsMultiplier = 1f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("rangeStun"), AutoNetworkedField]
    [Access(Other = AccessPermissions.ReadWrite)]
    public float RangeStun = 5f;

    [DataField("stunRadiusSound")]
    public SoundSpecifier? StunRadiusSound = default;

    [DataField("ignorAlien")]
    public bool IgnorAlien = true;
}
