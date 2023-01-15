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
    /// This is effectively hardfloored at <see cref="GravityWellSystem.MinGravWellRange"/>.
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
    /// The acceleration entities will experience relative to the gravity well at a distance of 1m.
    /// Left-multiplies with the entities displacement from the center of the gravity well.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Matrix3 MatrixAcceleration;
    
    /// <summary>
    /// The set of entities being affected by this gravity well.
    /// All entities within this group are accelerated relative to the gravity well.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [Access(typeof(GravityWellSystem))]
    public List<EntityUid> Captured = new();

    #region Update Timing

    /// <summary>
    /// The amount of time that should elapse between automated scans for captured entities.
    /// </summary>
    [DataField("gravPulsePeriod")]
    [ViewVariables(VVAccess.ReadOnly)]
    [Access(typeof(GravityWellSystem))]
    public TimeSpan TargetScanPeriod { get; internal set; } = TimeSpan.FromSeconds(0.5);

    /// <summary>
    /// The next time at which this gravity well scanned for captured entities.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [Access(typeof(GravityWellSystem))]
    public TimeSpan NextScanTime { get; internal set; } = default!;

    /// <summary>
    /// The last time this gravity well scanned for captured entities.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [Access(typeof(GravityWellSystem))]
    public TimeSpan LastScanTime { get; internal set; } = default!;

    #endregion Update Timing
}
