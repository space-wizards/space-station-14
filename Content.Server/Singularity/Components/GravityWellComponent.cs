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

    /// <summary>
    /// The amount of time between gravitational pulses this emits.
    /// </summary>
    [DataField("gravPulsePeriod")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float GravPulsePeriod = 0.5f;

    /// <summary>
    /// The time elapsed since the last gravitational pulse this emitted.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [Access(friends:typeof(GravityWellSystem))]
    public float TimeSinceLastGravPulse = 0.0f;
}
