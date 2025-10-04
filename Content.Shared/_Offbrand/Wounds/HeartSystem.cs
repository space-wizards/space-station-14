using System.Linq;
using Content.Shared._Offbrand.StatusEffects;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Medical;
using Content.Shared.Random.Helpers;
using Content.Shared.Rejuvenate;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Offbrand.Wounds;

public sealed partial class HeartSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PainSystem _pain = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HeartrateComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<HeartrateComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<HeartrateComponent, ApplyMetabolicMultiplierEvent>(OnApplyMetabolicMultiplier);
        SubscribeLocalEvent<HeartrateComponent, RejuvenateEvent>(OnRejuvenate);

        SubscribeLocalEvent<BloodstreamComponent, GetStrainEvent>(OnBloodstreamGetStrain);

        SubscribeLocalEvent<HeartStopOnHypovolemiaComponent, HeartBeatEvent>(OnHeartBeatHypovolemia);
        SubscribeLocalEvent<HeartStopOnHighStrainComponent, HeartBeatEvent>(OnHeartBeatStrain);
        SubscribeLocalEvent<HeartStopOnBrainHealthComponent, HeartBeatEvent>(OnHeartBeatBrain);

        SubscribeLocalEvent<HeartStopOnHypovolemiaComponent, BeforeTargetDefibrillatedEvent>(OnHeartBeatHypovolemiaMessage);
        SubscribeLocalEvent<HeartStopOnHighStrainComponent, BeforeTargetDefibrillatedEvent>(OnHeartBeatStrainMessage);
        SubscribeLocalEvent<HeartStopOnBrainHealthComponent, BeforeTargetDefibrillatedEvent>(OnHeartBeatBrainMessage);

        SubscribeLocalEvent<HeartDefibrillatableComponent, TargetDefibrillatedEvent>(OnTargetDefibrillated);
    }

    private void OnRejuvenate(Entity<HeartrateComponent> ent, ref RejuvenateEvent args)
    {
        ent.Comp.Damage = 0;
        ent.Comp.Running = true;
        ent.Comp.Strain = 0;
        Dirty(ent);

        var strainChangedEvt = new AfterStrainChangedEvent();
        RaiseLocalEvent(ent, ref strainChangedEvt);

        var overlays = new bPotentiallyUpdateDamageOverlayEventb(ent);
        RaiseLocalEvent(ent, ref overlays, true);
    }

    private void OnMapInit(Entity<HeartrateComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.LastUpdate = _timing.CurTime;
    }

    private void OnShutdown(Entity<HeartrateComponent> ent, ref ComponentShutdown args)
    {
        _statusEffects.TryRemoveStatusEffect(ent, ent.Comp.HeartStoppedEffect);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<HeartrateComponent>();
        while (query.MoveNext(out var uid, out var heartrate))
        {
            if (heartrate.LastUpdate is not { } last || last + heartrate.AdjustedUpdateInterval >= _timing.CurTime)
                continue;

            var delta = _timing.CurTime - last;
            heartrate.LastUpdate = _timing.CurTime;
            Dirty(uid, heartrate);

            if (!heartrate.Running)
                continue;

            var newStrain = RecomputeHeartStrain((uid, heartrate));
            if (newStrain != heartrate.Strain)
            {
                heartrate.Strain = RecomputeHeartStrain((uid, heartrate));
                Dirty(uid, heartrate);

                var strainChangedEvt = new AfterStrainChangedEvent();
                RaiseLocalEvent(uid, ref strainChangedEvt);
            }

            var evt = new HeartBeatEvent(false);
            RaiseLocalEvent(uid, ref evt);

            if (!evt.Stop)
            {
                var threshold = heartrate.StrainDamageThresholds.HighestMatch(HeartStrain((uid, heartrate)));
                if (threshold is (var chance, var amount))
                {
                    var seed = SharedRandomExtensions.HashCodeCombine(new() { (int)_timing.CurTick.Value, GetNetEntity(uid).Id });
                    var rand = new System.Random(seed);

                    if (rand.Prob(chance))
                    {
                        heartrate.Damage = FixedPoint2.Min(heartrate.Damage + amount, heartrate.MaxDamage);
                    }

                    if (heartrate.Damage >= heartrate.MaxDamage)
                    {
                        evt.Stop = true;
                    }
                    Dirty(uid, heartrate);
                }
            }

            if (evt.Stop)
            {
                StopHeart((uid, heartrate));
                continue;
            }

            var overlays = new bPotentiallyUpdateDamageOverlayEventb(uid);
            RaiseLocalEvent(uid, ref overlays, true);
        }
    }

    private void StopHeart(Entity<HeartrateComponent> ent)
    {
        ent.Comp.Running = false;
        ent.Comp.Strain = 0;

        var strainChangedEvt = new AfterStrainChangedEvent();
        RaiseLocalEvent(ent, ref strainChangedEvt);

        var stoppedEvt = new HeartStoppedEvent();
        RaiseLocalEvent(ent, ref stoppedEvt);

        _statusEffects.TryUpdateStatusEffectDuration(ent.Owner, ent.Comp.HeartStoppedEffect, out _);
    }

    public void KillHeart(Entity<HeartrateComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.Damage = ent.Comp.MaxDamage;
        ent.Comp.Running = false;
        ent.Comp.Strain = 0;
        Dirty(ent);

        var strainChangedEvt = new AfterStrainChangedEvent();
        RaiseLocalEvent(ent, ref strainChangedEvt);

        var stoppedEvt = new HeartStoppedEvent();
        RaiseLocalEvent(ent, ref stoppedEvt);

        _statusEffects.TryUpdateStatusEffectDuration(ent, ent.Comp.HeartStoppedEffect, out _);
    }

    private void OnApplyMetabolicMultiplier(Entity<HeartrateComponent> ent, ref ApplyMetabolicMultiplierEvent args)
    {
        ent.Comp.UpdateIntervalMultiplier = args.Multiplier;
        Dirty(ent);
    }

    private void OnHeartBeatHypovolemia(Entity<HeartStopOnHypovolemiaComponent> ent, ref HeartBeatEvent args)
    {
        var seed = SharedRandomExtensions.HashCodeCombine(new() { (int)_timing.CurTick.Value, GetNetEntity(ent).Id });
        var rand = new System.Random(seed);

        var volume = BloodVolume((ent.Owner, Comp<HeartrateComponent>(ent)));
        args.Stop = args.Stop || rand.Prob(ent.Comp.Chance) && volume < ent.Comp.VolumeThreshold;
    }

    private void OnHeartBeatStrain(Entity<HeartStopOnHighStrainComponent> ent, ref HeartBeatEvent args)
    {
        var seed = SharedRandomExtensions.HashCodeCombine(new() { (int)_timing.CurTick.Value, GetNetEntity(ent).Id });
        var rand = new System.Random(seed);

        if (_statusEffects.HasEffectComp<PreventHeartStopFromStrainStatusEffectComponent>(ent))
            return;

        var strain = HeartStrain((ent.Owner, Comp<HeartrateComponent>(ent)));
        args.Stop = args.Stop || rand.Prob(ent.Comp.Chance) && strain > ent.Comp.Threshold;
    }

    private void OnHeartBeatBrain(Entity<HeartStopOnBrainHealthComponent> ent, ref HeartBeatEvent args)
    {
        var seed = SharedRandomExtensions.HashCodeCombine(new() { (int)_timing.CurTick.Value, GetNetEntity(ent).Id });
        var rand = new System.Random(seed);

        if (_statusEffects.HasEffectComp<PreventHeartStopFromStrainStatusEffectComponent>(ent))
            return;

        var damage = Comp<BrainDamageComponent>(ent).Damage;
        args.Stop = args.Stop || rand.Prob(ent.Comp.Chance) && damage > ent.Comp.Threshold;
    }

    private void OnHeartBeatHypovolemiaMessage(Entity<HeartStopOnHypovolemiaComponent> ent, ref BeforeTargetDefibrillatedEvent args)
    {
        var volume = BloodVolume((ent.Owner, Comp<HeartrateComponent>(ent)));
        if (volume >= ent.Comp.VolumeThreshold)
            return;

        args.Messages.Add(ent.Comp.Warning);
    }

    private void OnHeartBeatStrainMessage(Entity<HeartStopOnHighStrainComponent> ent, ref BeforeTargetDefibrillatedEvent args)
    {
        if (_statusEffects.HasEffectComp<PreventHeartStopFromStrainStatusEffectComponent>(ent))
            return;

        var strain = RecomputeHeartStrain((ent.Owner, Comp<HeartrateComponent>(ent)));
        if (strain < ent.Comp.Threshold)
            return;

        args.Messages.Add(ent.Comp.Warning);
    }

    private void OnHeartBeatBrainMessage(Entity<HeartStopOnBrainHealthComponent> ent, ref BeforeTargetDefibrillatedEvent args)
    {
        if (_statusEffects.HasEffectComp<PreventHeartStopFromStrainStatusEffectComponent>(ent))
            return;

        var damage = Comp<BrainDamageComponent>(ent).Damage;
        if (damage <= ent.Comp.Threshold)
            return;

        args.Messages.Add(ent.Comp.Warning);
    }

    public void ChangeHeartDamage(Entity<HeartrateComponent?> ent, FixedPoint2 amount)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var newValue = FixedPoint2.Clamp(ent.Comp.Damage + amount, FixedPoint2.Zero, ent.Comp.MaxDamage);
        if (newValue == ent.Comp.Damage)
            return;

        ent.Comp.Damage = newValue;
        Dirty(ent);

        if (newValue >= ent.Comp.MaxDamage && ent.Comp.Running)
        {
            StopHeart((ent.Owner, ent.Comp));
        }
    }

    public FixedPoint2 BloodVolume(Entity<HeartrateComponent> ent)
    {
        var bloodstream = Comp<BloodstreamComponent>(ent);
        if (!_solutionContainer.ResolveSolution(ent.Owner, bloodstream.BloodSolutionName,
            ref bloodstream.BloodSolution, out var bloodSolution))
        {
            return 1;
        }

        return bloodSolution.Volume / bloodSolution.MaxVolume;
    }

    public FixedPoint4 BloodFlow(Entity<HeartrateComponent> ent)
    {
        if (!ent.Comp.Running)
        {
            var evt = new GetStoppedCirculationModifier(ent.Comp.StoppedBloodCirculationModifier);
            RaiseLocalEvent(ent, ref evt);
            return evt.Modifier;
        }

        FixedPoint4 modifier = 1;

        FixedPoint4 strain = HeartStrain(ent);

        var strainModifier = ent.Comp.CirculationStrainModifierCoefficient * strain + ent.Comp.CirculationStrainModifierConstant;

        modifier *= strainModifier;

        modifier *= FixedPoint2.Max( ent.Comp.MinimumDamageCirculationModifier, FixedPoint2.New(1d) - (ent.Comp.Damage / ent.Comp.MaxDamage) );

        return modifier;
    }

    public FixedPoint2 BloodCirculation(Entity<HeartrateComponent> ent)
    {
        FixedPoint4 volume = BloodVolume(ent);
        var flow = BloodFlow(ent);

        return FixedPoint2.Min((FixedPoint2)(volume * flow), 1);
    }

    public FixedPoint2 BloodOxygenation(Entity<HeartrateComponent> ent)
    {
        var circulation = BloodCirculation(ent);
        var damageable = Comp<DamageableComponent>(ent);
        if (!damageable.Damage.DamageDict.TryGetValue(ent.Comp.AsphyxiationDamage, out var asphyxiationAmount))
            return circulation;

        var oxygenationModifier = FixedPoint2.Clamp(1 - (asphyxiationAmount / ent.Comp.AsphyxiationThreshold), 0, 1);

        var evt = new GetOxygenationModifier(oxygenationModifier);
        RaiseLocalEvent(ent, ref evt);

        return evt.Modifier * circulation;
    }

    private FixedPoint2 RecomputeHeartStrain(Entity<HeartrateComponent> ent)
    {
        var pain = _pain.GetShock(ent.Owner);
        var strain = pain / ent.Comp.ShockStrainDivisor;

        var evt = new GetStrainEvent(strain);
        RaiseLocalEvent(ent, ref evt);

        return FixedPoint2.Clamp(evt.Strain, FixedPoint2.Zero, ent.Comp.MaximumStrain);
    }

    public FixedPoint2 HeartStrain(Entity<HeartrateComponent> ent)
    {
        return ent.Comp.Strain;
    }

    private void OnBloodstreamGetStrain(Entity<BloodstreamComponent> ent, ref GetStrainEvent args)
    {
        var heartrate = Comp<HeartrateComponent>(ent);
        var volume = BloodVolume((ent, heartrate));
        var damageable = Comp<DamageableComponent>(ent);
        if (damageable.Damage.DamageDict.TryGetValue(heartrate.AsphyxiationDamage, out var asphyxiationAmount))
        {
            volume *= FixedPoint2.Min(1 - (asphyxiationAmount / heartrate.AsphyxiationThreshold), 1);
        }

        var strainDelta = FixedPoint2.Zero;

        if (volume <= ent.Comp.BloodlossThreshold)
            strainDelta += 1;
        if (volume <= ent.Comp.BloodlossThreshold/2)
            strainDelta += 1;
        if (volume <= ent.Comp.BloodlossThreshold/3)
            strainDelta += 1;
        if (volume <= ent.Comp.BloodlossThreshold/4)
            strainDelta += 1;

        args.Strain += strainDelta;
    }

    public (FixedPoint2, FixedPoint2) BloodPressure(Entity<HeartrateComponent> ent)
    {
        var seed = SharedRandomExtensions.HashCodeCombine(new() { (int)_timing.CurTick.Value, GetNetEntity(ent).Id });
        var rand = new System.Random(seed);

        var volume = BloodCirculation(ent);

        var deviationA = rand.Next(-ent.Comp.BloodPressureDeviation, ent.Comp.BloodPressureDeviation);
        var deviationB = rand.Next(-ent.Comp.BloodPressureDeviation, ent.Comp.BloodPressureDeviation);

        var upper = FixedPoint2.Max((ent.Comp.SystolicBase * volume + deviationA), 0).Int();
        var lower = FixedPoint2.Max((ent.Comp.DiastolicBase * volume + deviationB), 0).Int();

        return (upper, lower);
    }

    public FixedPoint2 HeartRate(Entity<HeartrateComponent> ent)
    {
        if (!ent.Comp.Running)
            return 0;

        var seed = SharedRandomExtensions.HashCodeCombine(new() { (int)_timing.CurTick.Value, GetNetEntity(ent).Id });
        var rand = new System.Random(seed);

        var strain = HeartStrain(ent);
        return (FixedPoint2.Max(strain, 0)/ent.Comp.HeartRateStrainDivisor) * ent.Comp.HeartRateStrainFactor + ent.Comp.HeartRateBase + rand.Next(-ent.Comp.HeartRateDeviation, ent.Comp.HeartRateDeviation);
    }

    public void TryRestartHeart(Entity<HeartrateComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (ent.Comp.MaxDamage <= ent.Comp.Damage || ent.Comp.Running)
            return;

        ent.Comp.Running = true;
        Dirty(ent);

        _statusEffects.TryRemoveStatusEffect(ent.Owner, ent.Comp.HeartStoppedEffect);

        var evt = new HeartStartedEvent();
        RaiseLocalEvent(ent, ref evt);
    }

    private void OnTargetDefibrillated(Entity<HeartDefibrillatableComponent> ent, ref TargetDefibrillatedEvent args)
    {
        TryRestartHeart(ent.Owner);
    }

    public bool IsCritical(Entity<HeartrateComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        return !ent.Comp.Running;
    }
}
