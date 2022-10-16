using Content.Server.Physics.Controllers;

namespace Content.Server.Physics.Components;

/// <summary>
///     A component which makes its entity move around at random.
/// </summary>
[RegisterComponent]
public sealed class RandomWalkComponent : Component
{
    /// <summary>
    /// The minimum speed at which this entity will move.
    /// </summary>
    [DataField("minSpeed")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float MinSpeed = 7.5f;

    /// <summary>
    /// The maximum speed at which this entity will move.
    /// </summary>
    [DataField("maxSpeed")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float MaxSpeed = 10f;

    /// <summary>
    /// The amount of speed carried over when the speed updates.
    /// </summary>
    [DataField("maxSpeed")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float AccumulatorRatio = 0.0f;

    /// <summary>
    /// The minimum amount of time (in seconds) between speed updates.
    /// </summary>
    [DataField("minCooldown")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float MinStepCooldown = 2f;

    /// <summary>
    /// The maximum amount of time (in seconds) between speed updates.
    /// </summary>
    [DataField("maxCooldown")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float MaxStepCooldown = 5f;

    /// <summary>
    /// The amount of time until the next speed update.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [Access(friends:typeof(RandomWalkController))]
    public float _timeUntilNextStep = 0.0f;
}
