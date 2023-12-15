using Robust.Shared.GameStates;

namespace Content.Shared.Throwing;

/// <summary>
/// When thrown applies a specific angle to the thrown entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ThrowingAngleComponent : Component
{
    /// <summary>
    /// Do we apply throwing spin to the entity.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("angularVelocity"), AutoNetworkedField]
    public bool AngularVelocity;

    [ViewVariables(VVAccess.ReadWrite), DataField("angle"), AutoNetworkedField]
    public Angle Angle;
}
