using Content.Shared.Anomaly;
using Content.Shared.Anomaly.Components;

namespace Content.Server.Anomaly.Components;

/// <summary>
/// This is used for projectiles which affect anomalies through colliding with them.
/// </summary>
[RegisterComponent, Access(typeof(SharedAnomalySystem))]
public sealed class AnomalousParticleComponent : Component
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
}
