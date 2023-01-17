using Content.Shared.Anomaly;

namespace Content.Server.Anomaly.Components;

/// <summary>
/// This is used for projectiles which affect anomalies through colliding with them.
/// </summary>
[RegisterComponent]
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
}
