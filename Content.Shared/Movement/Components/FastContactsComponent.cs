using Content.Shared.Movement.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

[NetworkedComponent, RegisterComponent]
[AutoGenerateComponentState]
[Access(typeof(FastContactsSystem))]
public sealed partial class FastContactsComponent : Component
{
    [DataField("walkSpeedModifier"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float WalkSpeedModifier = 1.2f;

    [AutoNetworkedField]
    [DataField("sprintSpeedModifier"), ViewVariables(VVAccess.ReadWrite)]
    public float SprintSpeedModifier = 1.2f;

}
