using Content.Shared.Singularity.Components;

namespace Content.Server.Singularity.Components;

[RegisterComponent]
[ComponentReference(typeof(SharedGravityWellComponent))]
public sealed class GravityWellComponent : SharedGravityWellComponent
{
    /// <summary>
    ///     The maximum range at which the gravity well can push/pull entities.
    /// </summary>
    [DataField("maxRange")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float MaxRange;

    /// <summary>
    ///     The minimum range at which the gravity well can push/pull entities.
    /// </summary>
    [DataField("maxRange")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float MinRange;

    /// <summary>
    ///     The acceleration entities will experience towards the gravity well at a distance of 1m.
    /// </summary>
    [DataField("maxRange")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float BaseRadialAcceleration = 10.0f;

    /// <summary>
    ///     The acceleration entities will experience tangent to the gravity well at a distance of 1m.
    /// </summary>
    [DataField("maxRange")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float BaseTangentialAcceleration = 0.0f;

    /// <summary>
    ///     The amount of time between gravitational pulses emitted by the owning entity.
    /// </summary>
    [DataField("gravPulsePeriod")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float GravPulsePeriod = 0.5f;

    /// <summary>
    ///     The amount of time until the next gravitational pulse emitted by the owning entity.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public float _timeSinceLastGravPulse = 0.0f;
}
