using Content.Server.Anomaly.Components;
using Content.Shared.Anomaly.Components;
using Robust.Shared.Random;

namespace Content.Server.Anomaly.Effects;

public sealed class ShuffleParticlesAnomalySystem : EntitySystem
{
    [Dependency] private readonly AnomalySystem _anomaly = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ShuffleParticlesAnomalyComponent, AnomalyPulseEvent>(OnPulse);
        SubscribeLocalEvent<ShuffleParticlesAnomalyComponent, AnomalyAffectedByParticleEvent>(OnAffectedByParticle);
    }

    private void OnAffectedByParticle(Entity<ShuffleParticlesAnomalyComponent> ent, ref AnomalyAffectedByParticleEvent args)
    {
        if (!TryComp<AnomalyComponent>(ent, out var anomalyComp))
            return;

        if (ent.Comp.ShuffleOnParticleHit && _random.Prob(ent.Comp.Prob))
            _anomaly.ShuffleParticlesEffect((args.Anomaly, anomalyComp));
    }

    private void OnPulse(Entity<ShuffleParticlesAnomalyComponent> ent, ref AnomalyPulseEvent args)
    {
        if (!TryComp<AnomalyComponent>(ent, out var anomaly))
            return;

        if (ent.Comp.ShuffleOnPulse && _random.Prob(ent.Comp.Prob))
        {
            _anomaly.ShuffleParticlesEffect((ent, anomaly));
        }
    }
}

