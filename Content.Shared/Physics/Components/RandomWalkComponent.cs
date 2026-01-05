using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using System.Numerics;

namespace Content.Shared.Physics.Components;

/// <summary>
/// A component which makes its entity move around at random.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RandomWalkComponent : Component
{
    /// <summary>
    /// The minimum speed at which this entity will move.
    /// </summary>
    [DataField]
    public float MinSpeed = 7.5f;

    /// <summary>
    /// The maximum speed at which this entity will move.
    /// </summary>
    [DataField]
    public float MaxSpeed = 10f;

    /// <summary>
    /// The amount of speed carried over when the speed updates.
    /// </summary>
    [DataField]
    public float AccumulatorRatio = 0.0f;

    /// <summary>
    /// The vector by which the random walk direction is biased.
    /// </summary>
    [DataField]
    public Vector2 BiasVector = new Vector2(0f, 0f);

    /// <summary>
    /// Whether to set BiasVector to (0, 0) every random walk update.
    /// </summary>
    [DataField]
    public bool ResetBiasOnWalk = true;

    /// <summary>
    /// Whether this random walker should take a step immediately when it starts up.
    /// </summary>
    [DataField]
    public bool StepOnStartup = false;

    #region Update Timing

    /// <summary>
    /// The minimum amount of time between speed updates.
    /// </summary>
    [DataField]
    public TimeSpan MinStepCooldown { get; internal set; } = TimeSpan.FromSeconds(2.0);

    /// <summary>
    /// The maximum amount of time between speed updates.
    /// </summary>
    [DataField]
    public TimeSpan MaxStepCooldown { get; internal set; } = TimeSpan.FromSeconds(5.0);

    /// <summary>
    /// The next time this should update its speed.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextStepTime = default!;

    #endregion Update Timing
}
