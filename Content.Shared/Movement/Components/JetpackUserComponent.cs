using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

/// <summary>
/// Added to someone using a jetpack for movement purposes
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class JetpackUserComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Jetpack;

    [DataField, AutoNetworkedField]
    public float WeightlessAcceleration = 1f;

    [DataField, AutoNetworkedField]
    public float WeightlessFriction = 0.3f;

    [DataField, AutoNetworkedField]
    public float WeightlessFrictionNoInput = 0.3f;

    [DataField, AutoNetworkedField]
    public float WeightlessModifier = 1.2f;
}
