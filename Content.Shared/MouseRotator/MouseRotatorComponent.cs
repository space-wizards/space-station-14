using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.MouseRotator;

/// <summary>
/// This component allows overriding an entities local rotation based on the client's mouse movement
/// </summary>
/// <see cref="SharedMouseRotatorSystem"/>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MouseRotatorComponent : Component
{
    /// <summary>
    ///     How much the desired angle needs to change before a predictive event is sent
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public Angle AngleTolerance = Angle.FromDegrees(5.0);

    /// <summary>
    ///     The angle that will be lerped to
    /// </summary>
    [AutoNetworkedField, DataField]
    public Angle? GoalRotation;

    /// <summary>
    ///     Max degrees the entity can rotate per second
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public double RotationSpeed = float.MaxValue;
}

/// <summary>
///     Raised on an entity with <see cref="MouseRotatorComponent"/> as a predictive event on the client
///     when mouse rotation changes
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestMouseRotatorRotationEvent : EntityEventArgs
{
    public Angle Rotation;
}
