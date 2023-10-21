using Content.Server.Electrocution;
using Content.Server.Emp;
using Content.Server.Lightning;
using Content.Server.Power.Components;
using Content.Shared.Anomaly.Components;
using Content.Shared.Anomaly.Effects.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.StatusEffect;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Anomaly.Effects;

public sealed class ElectricityAnomalySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly LightningSystem _lightning = default!;
    [Dependency] private readonly ElectrocutionSystem _electrocution = default!;
    [Dependency] private readonly EmpSystem _emp = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ElectricityAnomalyComponent, AnomalyPulseEvent>(OnPulse);
        SubscribeLocalEvent<ElectricityAnomalyComponent, AnomalySupercriticalEvent>(OnSupercritical);
    }

    private void OnPulse(EntityUid uid, ElectricityAnomalyComponent component, ref AnomalyPulseEvent args)
    {
        var range = component.MaxElectrocuteRange * args.Stability;
        var xform = Transform(uid);
        foreach (var (ent, comp) in _lookup.GetEntitiesInRange<MobStateComponent>(xform.MapPosition, range))
        {
            _lightning.ShootLightning(uid, ent);
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

            if (_random.Prob(0.01f) && poweredQuery.HasComponent(ent))
                validEnts.Add(ent);
        }

        // goodbye, sweet perf
        foreach (var ent in validEnts)
        {
            _lightning.ShootLightning(uid, ent);
        }

        var empRange = component.MaxElectrocuteRange * 3;
        _emp.EmpPulse(Transform(uid).MapPosition, empRange, component.EmpEnergyConsumption, component.EmpDisabledDuration);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ElectricityAnomalyComponent, AnomalyComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var elec, out var anom, out var xform))
        {
            if (_timing.CurTime < elec.NextSecond)
                continue;
            elec.NextSecond = _timing.CurTime + TimeSpan.FromSeconds(1);

            if (!_random.Prob(elec.PassiveElectrocutionChance * anom.Stability))
                continue;

            var range = elec.MaxElectrocuteRange * anom.Stability;
            var damage = (int) (elec.MaxElectrocuteDamage * anom.Severity);
            var duration = elec.MaxElectrocuteDuration * anom.Severity;

            foreach (var (ent, comp) in _lookup.GetEntitiesInRange<StatusEffectsComponent>(xform.MapPosition, range))
            {
                _electrocution.TryDoElectrocution(ent, uid, damage, duration, true, statusEffects: comp, ignoreInsulation: true);
            }
        }
    }
}
