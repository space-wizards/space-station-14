using Content.Shared.Singularity.Components;
using Content.Server.Singularity.EntitySystems;

namespace Content.Server.Singularity.Components;

/// <summary>
/// The server-side version of <see cref="SharedGravityWellComponent"/>.
/// Primarily managed by <see cref="GravityWellSystem"/>.
/// </summary>
[RegisterComponent]
[ComponentReference(typeof(SharedGravityWellComponent))]
public sealed class GravityWellComponent : SharedGravityWellComponent
{
    /// <summary>
    /// The maximum range at which the gravity well can push/pull entities.
    /// </summary>
    [DataField("maxRange")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float MaxRange;

    /// <summary>
    /// The minimum range at which the gravity well can push/pull entities.
    /// </summary>
    [DataField("minRange")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float MinRange;

    /// <summary>
    /// The acceleration entities will experience towards the gravity well at a distance of 1m.
    /// Actual acceleration scales with the inverse of the distance to the singularity.
    /// </summary>
    [DataField("baseRadialAcceleration")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float BaseRadialAcceleration = 10.0f;

    /// <summary>
    /// The acceleration entities will experience tangent to the gravity well at a distance of 1m.
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
    public float _timeSinceLastGravPulse = 0.0f;
}
