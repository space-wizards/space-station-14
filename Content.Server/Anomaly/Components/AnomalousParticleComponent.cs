using Content.Shared.Anomaly;
using Content.Shared.Anomaly.Components;

namespace Content.Server.Anomaly.Components;

/// <summary>
/// This is used for projectiles which affect anomalies through colliding with them.
/// </summary>
[RegisterComponent, Access(typeof(SharedAnomalySystem))]
public sealed partial class AnomalousParticleComponent : Component
{
    /// <summary>
    /// The type of particle that the projectile
    /// imbues onto the anomaly on contact.
    /// </summary>
    [DataField("particleType", required: true)]
    public AnomalousParticleType ParticleType;

    /// <summary>
    /// The fixture that's checked on collision.
    /// </summary>
    [DataField("fixtureId")]
    public string FixtureId = "projectile";

    /// <summary>
    /// The amount that the <see cref="AnomalyComponent.Severity"/> increases by when hit
    /// of an anomalous particle of <seealso cref="AnomalyComponent.SeverityParticleType"/>.
    /// </summary>
    [DataField("severityPerSeverityHit")]
    public float SeverityPerSeverityHit = 0.025f;

    /// <summary>
    /// The amount that the <see cref="AnomalyComponent.Stability"/> increases by when hit
    /// of an anomalous particle of <seealso cref="AnomalyComponent.DestabilizingParticleType"/>.
    /// </summary>
    [DataField("stabilityPerDestabilizingHit")]
    public float StabilityPerDestabilizingHit = 0.04f;

    /// <summary>
    /// The amount that the <see cref="AnomalyComponent.Stability"/> increases by when hit
    /// of an anomalous particle of <seealso cref="AnomalyComponent.DestabilizingParticleType"/>.
    /// </summary>
    [DataField("healthPerWeakeningeHit")]
    public float HealthPerWeakeningeHit = -0.05f;

    /// <summary>
    /// The amount that the <see cref="AnomalyComponent.Stability"/> increases by when hit
    /// of an anomalous particle of <seealso cref="AnomalyComponent.DestabilizingParticleType"/>.
    /// </summary>
    [DataField("stabilityPerWeakeningeHit")]
    public float StabilityPerWeakeningeHit = -0.1f;

    /// <summary>
    /// If this is true then the particle will always affect the stability of the anomaly.
    /// </summary>
    [DataField("destabilzingOverride")]
    public bool DestabilzingOverride = false;

    /// <summary>
    /// If this is true then the particle will always affect the weakeness of the anomaly.
    /// </summary>
    [DataField("weakeningOverride")]
    public bool WeakeningOverride = false;

    /// <summary>
    /// If this is true then the particle will always affect the severity of the anomaly.
    /// </summary>
    [DataField("severityOverride")]
    public bool SeverityOverride = false;
}
