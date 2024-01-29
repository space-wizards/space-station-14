using Content.Server.Anomaly.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Audio;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Materials;
using Content.Server.Radiation.Systems;
using Content.Server.Radio.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.Anomaly;
using Content.Shared.Anomaly.Components;
using Content.Shared.DoAfter;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Anomaly;

/// <summary>
/// This handles logic and interactions relating to <see cref="AnomalyComponent"/>
/// </summary>
public sealed partial class AnomalySystem : SharedAnomalySystem
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly AmbientSoundSystem _ambient = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly MaterialStorageSystem _material = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly RadiationSystem _radiation = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public const float MinParticleVariation = 0.8f;
    public const float MaxParticleVariation = 1.2f;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AnomalyComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AnomalyComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<AnomalyComponent, StartCollideEvent>(OnStartCollide);

        InitializeGenerator();
        InitializeScanner();
        InitializeVessel();
        InitializeCommands();
    }

    private void OnMapInit(EntityUid uid, AnomalyComponent component, MapInitEvent args)
    {
        component.NextPulseTime = Timing.CurTime + GetPulseLength(component) * 3; // longer the first time
        ChangeAnomalyStability(uid, Random.NextFloat(component.InitialStabilityRange.Item1 , component.InitialStabilityRange.Item2), component);
        ChangeAnomalySeverity(uid, Random.NextFloat(component.InitialSeverityRange.Item1, component.InitialSeverityRange.Item2), component);

        var particles = new List<AnomalousParticleType>
            { AnomalousParticleType.Delta, AnomalousParticleType.Epsilon, AnomalousParticleType.Zeta };
        component.SeverityParticleType = Random.PickAndTake(particles);
        component.DestabilizingParticleType = Random.PickAndTake(particles);
        component.WeakeningParticleType = Random.PickAndTake(particles);
    }

    private void OnShutdown(EntityUid uid, AnomalyComponent component, ComponentShutdown args)
    {
        EndAnomaly(uid, component);
    }

    private void OnStartCollide(EntityUid uid, AnomalyComponent component, ref StartCollideEvent args)
    {
        if (!TryComp<AnomalousParticleComponent>(args.OtherEntity, out var particle))
            return;

        if (args.OtherFixtureId != particle.FixtureId)
            return;

        // small function to randomize because it's easier to read like this
        float VaryValue(float v) => v * Random.NextFloat(MinParticleVariation, MaxParticleVariation);

        if (particle.ParticleType == component.DestabilizingParticleType || particle.DestabilzingOverride)
        {
            ChangeAnomalyStability(uid, VaryValue(particle.StabilityPerDestabilizingHit), component);
        }
        if (particle.ParticleType == component.SeverityParticleType || particle.SeverityOverride)
        {
            ChangeAnomalySeverity(uid, VaryValue(particle.SeverityPerSeverityHit), component);
        }
        if (particle.ParticleType == component.WeakeningParticleType || particle.WeakeningOverride)
        {
            ChangeAnomalyHealth(uid, VaryValue(particle.HealthPerWeakeningeHit), component);
            ChangeAnomalyStability(uid, VaryValue(particle.StabilityPerWeakeningeHit), component);
        }
    }

    /// <summary>
    /// Gets the amount of research points generated per second for an anomaly.
    /// </summary>
    /// <param name="anomaly"></param>
    /// <param name="component"></param>
    /// <returns>The amount of points</returns>
    public int GetAnomalyPointValue(EntityUid anomaly, AnomalyComponent? component = null)
    {
        if (!Resolve(anomaly, ref component, false))
            return 0;

        var multiplier = 1f;
        if (component.Stability > component.GrowthThreshold)
            multiplier = component.GrowingPointMultiplier; //more points for unstable

        //penalty of up to 50% based on health
        multiplier *= MathF.Pow(1.5f, component.Health) - 0.5f;

        var severityValue = 1 / (1 + MathF.Pow(MathF.E, -7 * (component.Severity - 0.5f)));

        return (int) ((component.MaxPointsPerSecond - component.MinPointsPerSecond) * severityValue * multiplier) + component.MinPointsPerSecond;
    }

    /// <summary>
    /// Gets the localized name of a particle.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public string GetParticleLocale(AnomalousParticleType type)
    {
        return type switch
        {
            AnomalousParticleType.Delta => Loc.GetString("anomaly-particles-delta"),
            AnomalousParticleType.Epsilon => Loc.GetString("anomaly-particles-epsilon"),
            AnomalousParticleType.Zeta => Loc.GetString("anomaly-particles-zeta"),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateGenerator();
        UpdateVessels();
    }
}
