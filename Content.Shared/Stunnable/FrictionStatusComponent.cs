using Robust.Shared.GameStates;

namespace Content.Shared.Stunnable;

/// <summary>
/// This is used to apply a friction modifier to an entity temporarily
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedStunSystem))]
public sealed partial class FrictionStatusComponent : Component
{
    [DataField, AutoNetworkedField]
    public float FrictionModifier = 1f;

    [DataField, AutoNetworkedField]
    public float AccelerationModifier = 1f;
}
