using Content.Server.Electrocution;
using Content.Server.Lightning;
using Content.Server.Power.Components;
using Content.Shared.Anomaly.Components;
using Content.Shared.Anomaly.Effects.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.StatusEffect;
using Robust.Shared.Random;

namespace Content.Server.Anomaly.Effects;

public sealed class ElectricityAnomalySystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly LightningSystem _lightning = default!;
    [Dependency] private readonly ElectrocutionSystem _electrocution = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ElectricityAnomalyComponent, AnomalyPulseEvent>(OnPulse);
        SubscribeLocalEvent<ElectricityAnomalyComponent, AnomalySupercriticalEvent>(OnSupercritical);
    }

    private void OnPulse(EntityUid uid, ElectricityAnomalyComponent component, ref AnomalyPulseEvent args)
    {
        var range = component.MaxElectrocuteRange * args.Stabiltiy;
        var damage = (int) (component.MaxElectrocuteDamage * args.Severity);
        var duration = component.MaxElectrocuteDuration * args.Severity;

        var xform = Transform(uid);
        foreach (var comp in _lookup.GetComponentsInRange<StatusEffectsComponent>(xform.MapPosition, range))
        {
            var ent = comp.Owner;

            _electrocution.TryDoElectrocution(ent, uid, damage, duration, true, statusEffects: comp, ignoreInsulation: true);
        }
    }

    private void OnSupercritical(EntityUid uid, ElectricityAnomalyComponent component, ref AnomalySupercriticalEvent args)
    {
        var poweredQuery = GetEntityQuery<ApcPowerReceiverComponent>();
        var mobQuery = GetEntityQuery<MobThresholdsComponent>();
        var validEnts = new HashSet<EntityUid>();
        foreach (var ent in _lookup.GetEntitiesInRange(uid, component.MaxElectrocuteRange * 2))
        {
            if (mobQuery.HasComponent(ent))
                validEnts.Add(ent);

            if (_random.Prob(0.1f) && poweredQuery.HasComponent(ent))
                validEnts.Add(ent);
        }

        // goodbye, sweet perf
        foreach (var ent in validEnts)
        {
            _lightning.ShootLightning(uid, ent);
        }
    }
}
