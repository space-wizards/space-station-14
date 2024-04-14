using Content.Server.Anomaly.Components;
using Content.Shared.Anomaly.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;

namespace Content.Server.Anomaly.Effects;
public sealed class ShuffleParticlesAnomalySystem : EntitySystem
{
    [Dependency] private readonly AnomalySystem _anomaly = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ShuffleParticlesAnomalyComponent, AnomalyPulseEvent>(OnPulse);
        SubscribeLocalEvent<ShuffleParticlesAnomalyComponent, StartCollideEvent>(OnStartCollide);
    }

    private void OnStartCollide(EntityUid uid, ShuffleParticlesAnomalyComponent shuffle, StartCollideEvent args)
    {
        if (!TryComp<AnomalyComponent>(uid, out var anomaly))
            return;

        if (shuffle.ShuffleOnParticleHit && _random.Prob(shuffle.Prob))
            _anomaly.ShuffleParticlesEffect(anomaly);

        if (!TryComp<AnomalousParticleComponent>(args.OtherEntity, out var particle))
            return;
    }

    private void OnPulse(EntityUid uid, ShuffleParticlesAnomalyComponent shuffle, AnomalyPulseEvent args)
    {
        if (!TryComp<AnomalyComponent>(uid, out var anomaly))
            return;

        if (shuffle.ShuffleOnPulse && _random.Prob(shuffle.Prob))
        {
            _anomaly.ShuffleParticlesEffect(anomaly);
        }
    }
}

