using Content.Shared.Movement.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

[NetworkedComponent, RegisterComponent]
[AutoGenerateComponentState]
[Access(typeof(FrictionContactsSystem))]
public sealed partial class FrictionContactsComponent : Component
{
    [DataField("mobFriction"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float MobFriction = 0.5f;

    [AutoNetworkedField]
    [DataField("mobFrictionNoInput"), ViewVariables(VVAccess.ReadWrite)]
    public float MobFrictionNoInput = 0.05f;

    [AutoNetworkedField]
    [DataField("mobAcceleration"), ViewVariables(VVAccess.ReadWrite)]
    public float MobAcceleration = 2.0f;
}
