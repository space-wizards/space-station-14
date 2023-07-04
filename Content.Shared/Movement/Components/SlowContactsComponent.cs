using Content.Shared.Movement.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

[NetworkedComponent, RegisterComponent]
[AutoGenerateComponentState]
[Access(typeof(SlowContactsSystem))]
public sealed partial class SlowContactsComponent : Component
{
    [DataField("walkSpeedModifier"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float WalkSpeedModifier = 1.0f;

    [AutoNetworkedField]
    [DataField("sprintSpeedModifier"), ViewVariables(VVAccess.ReadWrite)]
    public float SprintSpeedModifier = 1.0f;

    [DataField("spiderWalkSpeedModifier"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float SpiderWalkSpeedModifier = 1.5f;

    [AutoNetworkedField]
    [DataField("spiderSprintSpeedModifier"), ViewVariables(VVAccess.ReadWrite)]
    public float SpiderSprintSpeedModifier = 1.5f;

    [DataField("ignoreWhitelist")]
    public EntityWhitelist? IgnoreWhitelist;
}
