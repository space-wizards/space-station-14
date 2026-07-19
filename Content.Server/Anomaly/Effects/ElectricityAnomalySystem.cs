using Content.Server.Electrocution;
using Content.Server.Emp;
using Content.Server.Lightning;
using Content.Shared.Anomaly.Components;
using Content.Shared.Anomaly.Effects.Components;
using Content.Shared.StatusEffect;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Anomaly.Effects;

public sealed partial class ElectricityAnomalySystem : EntitySystem
{
    private static readonly EntityTimerId PassiveElectrocutionTimer = new("passive-electrocution");

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private LightningSystem _lightning = default!;
    [Dependency] private ElectrocutionSystem _electrocution = default!;
    [Dependency] private EmpSystem _emp = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private EntityTimerSystem _timers = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ElectricityAnomalyComponent, AnomalyPulseEvent>(OnPulse);
        SubscribeLocalEvent<ElectricityAnomalyComponent, AnomalySupercriticalEvent>(OnSupercritical);
        SubscribeLocalEvent<ElectricityAnomalyComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ElectricityAnomalyComponent, EntityTimerEvent>(OnTimer);
    }

    private void OnMapInit(Entity<ElectricityAnomalyComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextSecond = _timing.CurTime;
        _timers.SetTimerAt(ent, PassiveElectrocutionTimer, ent.Comp.NextSecond, TimeSpan.FromSeconds(1));
    }

    private void OnPulse(Entity<ElectricityAnomalyComponent> anomaly, ref AnomalyPulseEvent args)
    {
        var range = anomaly.Comp.MaxElectrocuteRange * args.Stability * args.PowerModifier;

        int boltCount = (int)MathF.Floor(MathHelper.Lerp((float)anomaly.Comp.MinBoltCount, (float)anomaly.Comp.MaxBoltCount, args.Severity));

        _lightning.ShootRandomLightnings(anomaly, range, boltCount);
    }

    private void OnSupercritical(Entity<ElectricityAnomalyComponent> anomaly, ref AnomalySupercriticalEvent args)
    {
        var range = anomaly.Comp.MaxElectrocuteRange * 3 * args.PowerModifier;

        _emp.EmpPulse(_transform.GetMapCoordinates(anomaly), range, anomaly.Comp.EmpEnergyConsumption, anomaly.Comp.EmpDisabledDuration);
        _lightning.ShootRandomLightnings(anomaly, range, anomaly.Comp.MaxBoltCount * 3, arcDepth: 3);
    }

    private void OnTimer(Entity<ElectricityAnomalyComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != PassiveElectrocutionTimer ||
            !TryComp<AnomalyComponent>(ent, out var anomaly) ||
            !TryComp<TransformComponent>(ent, out var xform))
            return;

        ent.Comp.NextSecond = args.NextDeadline ?? args.FiredAt;
        if (!_random.Prob(ent.Comp.PassiveElectrocutionChance * anomaly.Stability))
            return;

        var range = ent.Comp.MaxElectrocuteRange * anomaly.Stability;
        var damage = (int) (ent.Comp.MaxElectrocuteDamage * anomaly.Severity);
        var duration = ent.Comp.MaxElectrocuteDuration * anomaly.Severity;

        foreach (var (target, status) in _lookup.GetEntitiesInRange<StatusEffectsComponent>(_transform.GetMapCoordinates(ent, xform), range))
            _electrocution.TryDoElectrocution(target, ent, damage, duration, true, statusEffects: status, ignoreInsulation: true);
    }
}
