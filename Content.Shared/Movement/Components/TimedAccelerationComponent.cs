using System.Numerics;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Movement.Components;

/// <summary>
/// Makes an entity accelerate over time.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class TimedAccelerationComponent : Component
{
    /// <summary>
    /// When acceleration should start (inclusive)
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan StartAcceleration;

    /// <summary>
    /// When acceleration should end (inclusive)
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan EndAcceleration;

    /// <summary>
    /// How long acceleration will last.
    /// </summary>
    public TimeSpan AccelerationTime
    {
        get => EndAcceleration - StartAcceleration;
        set => EndAcceleration = StartAcceleration + value;
    }

    /// <summary>
    /// After the acceleration, does this component get removed?
    /// </summary>
    [DataField]
    public bool RemoveOnEnd;

    /// <summary>
    /// Not affected by entity rotation.
    /// </summary>
    [DataField]
    public Vector2 WorldAcceleration;

    /// <summary>
    /// Affected by entity rotation.
    /// </summary>
    [DataField]
    public Vector2 LocalAcceleration;
}
