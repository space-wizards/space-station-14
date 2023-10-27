using Content.Shared.Movement.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

[NetworkedComponent, RegisterComponent]
[AutoGenerateComponentState]
[Access(typeof(FrictionContactsSystem))]
public sealed partial class FrictionContactsComponent : Component
{
    /// <summary>
    /// Modified mob friction while on FrictionContactsComponent
    /// </summary>
    [DataField("mobFriction"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float MobFriction = 0.5f;

    /// <summary>
    /// Modified mob friction without input while on FrictionContactsComponent
    /// </summary>
    [AutoNetworkedField]
    [DataField("mobFrictionNoInput"), ViewVariables(VVAccess.ReadWrite)]
    public float MobFrictionNoInput = 0.05f;

    /// <summary>
    /// Modified mob acceleration while on FrictionContactsComponent
    /// </summary>
    [AutoNetworkedField]
    [DataField("mobAcceleration"), ViewVariables(VVAccess.ReadWrite)]
    public float MobAcceleration = 2.0f;
}
