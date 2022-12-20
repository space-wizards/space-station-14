using Content.Shared.Singularity.Components;
using Content.Server.Singularity.EntitySystems;

namespace Content.Server.Singularity.Components;

/// <summary>
/// The server-side version of <see cref="SharedGravityWellComponent"/>.
/// Primarily managed by <see cref="GravityWellSystem"/>.
/// </summary>
[RegisterComponent]
public sealed class GravityWellComponent : Component
{
    /// <summary>
    /// The maximum range at which the gravity well can push/pull entities.
    /// </summary>
    [DataField("maxRange")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float MaxRange;

    /// <summary>
    /// The minimum range at which the gravity well can push/pull entities.
    /// This is effectively hardfloored at <see cref="GravityWellSystem.MinGravPulseRange"/>.
    /// </summary>
    [DataField("minRange")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float MinRange = 0f;

    /// <summary>
    /// The acceleration entities will experience towards the gravity well at a distance of 1m.
    /// Negative values accelerate entities away from the gravity well.
    /// Actual acceleration scales with the inverse of the distance to the singularity.
    /// </summary>
    [DataField("baseRadialAcceleration")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float BaseRadialAcceleration = 0.0f;

    /// <summary>
    /// The acceleration entities will experience tangent to the gravity well at a distance of 1m.
    /// Positive tangential acceleration is counter-clockwise.
    /// Actual acceleration scales with the inverse of the distance to the singularity.
    /// </summary>
    [DataField("baseTangentialAcceleration")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float BaseTangentialAcceleration = 0.0f;

    #region Update Timing

    /// <summary>
    /// The amount of time that should elapse between automated updates to this gravity well.
    /// </summary>
    [DataField("gravPulsePeriod")]
    [ViewVariables(VVAccess.ReadOnly)]
    [Access(typeof(GravityWellSystem))]
    public TimeSpan TargetPulsePeriod { get; internal set; } = TimeSpan.FromSeconds(0.5);

    /// <summary>
    /// The next time at which this gravity well should pulse.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [Access(typeof(GravityWellSystem))]
    public TimeSpan NextPulseTime { get; internal set; } = default!;

    /// <summary>
    /// The last time this gravity well pulsed.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [Access(typeof(GravityWellSystem))]
    public TimeSpan LastPulseTime { get; internal set; } = default!;

    #endregion Update Timing
}
