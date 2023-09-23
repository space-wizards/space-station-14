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
    [DataField, AutoNetworkedField]
    public Angle AngleTolerance = Angle.FromDegrees(20.0);

    /// <summary>
    ///     The angle that will be lerped to
    /// </summary>
    [DataField, AutoNetworkedField]
    public Angle? GoalRotation;

    /// <summary>
    ///     Max degrees the entity can rotate per second
    /// </summary>
    [DataField, AutoNetworkedField]
    public double RotationSpeed = float.MaxValue;

    /// <summary>
    ///     This one is important. If this is true, <see cref="AngleTolerance"/> does not apply, and the system will
    ///     use <see cref="RequestMouseRotatorRotationSimpleEvent"/> instead. In this mode, the client will only send
    ///     events when an entity should snap to a different cardinal direction, rather than for every angle change.
    ///
    ///     This is useful for cases like humans, where what really matters is the visual sprite direction, as opposed to something
    ///     like turrets or ship guns, which have finer range of movement.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Simple4DirMode = true;
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

/// <summary>
///     Simpler version of <see cref="RequestMouseRotatorRotationEvent"/> for implementations
///     that only require snapping to 4-dir and not full angle rotation.
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestMouseRotatorRotationSimpleEvent : EntityEventArgs
{
    public Direction Direction;
}
