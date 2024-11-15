using Content.Shared.Movement.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

[NetworkedComponent, RegisterComponent]
[AutoGenerateComponentState]
[Access(typeof(SpeedModifierContactsSystem))]
public sealed partial class SpeedModifierContactsComponent : Component
{
    [DataField("walkSpeedModifier"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float WalkSpeedModifier = 1.0f;

    [DataField("sprintSpeedModifier"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float SprintSpeedModifier = 1.0f;

    [DataField("affectAirborne"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public bool AffectAirborne = false;

    [DataField("ignoreWhitelist")]
    public EntityWhitelist? IgnoreWhitelist;
}
