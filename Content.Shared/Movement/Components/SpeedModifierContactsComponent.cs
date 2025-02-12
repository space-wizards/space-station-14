using Content.Shared.Movement.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

[NetworkedComponent, RegisterComponent]
[AutoGenerateComponentState]
[Access(typeof(SpeedModifierContactsSystem))]
public sealed partial class SpeedModifierContactsComponent : Component
{
    [DataField, ViewVariables]
    [AutoNetworkedField]
    public float WalkSpeedModifier = 1.0f;

    [AutoNetworkedField]
    [DataField, ViewVariables]
    public float SprintSpeedModifier = 1.0f;

    [DataField("ignoreWhitelist")]
    public EntityWhitelist? IgnoreWhitelist;
}
