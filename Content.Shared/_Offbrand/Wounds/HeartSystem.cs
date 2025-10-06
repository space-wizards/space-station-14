using System.Linq;
using Content.Shared._Offbrand.StatusEffects;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Medical;
using Content.Shared.Random.Helpers;
using Content.Shared.Rejuvenate;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Maths;
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

        SubscribeLocalEvent<HeartStopOnHighStrainComponent, HeartBeatEvent>(OnHeartBeatStrain);
        SubscribeLocalEvent<HeartStopOnHighStrainComponent, BeforeTargetDefibrillatedEvent>(OnHeartBeatStrainMessage);

        SubscribeLocalEvent<HeartDefibrillatableComponent, TargetDefibrillatedEvent>(OnTargetDefibrillated);
    }

    private void OnRejuvenate(Entity<HeartrateComponent> ent, ref RejuvenateEvent args)
    {
        ent.Comp.Damage = 0;
        ent.Comp.Running = true;
        Dirty(ent);

        var overlays = new PotentiallyUpdateDamageOverlayEvent(ent);
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

            RecomputeVitals((uid, heartrate));

            var strainChanged = new AfterStrainChangedEvent();
            RaiseLocalEvent(uid, ref strainChanged);

            var respiration = new ApplyRespiratoryRateModifiersEvent(ComputeRespiratoryRateModifier((uid, heartrate)), ComputeExhaleEfficiencyModifier((uid, heartrate)));
            RaiseLocalEvent(uid, ref respiration);

            if (!heartrate.Running)
                continue;

            var evt = new HeartBeatEvent(false);
            RaiseLocalEvent(uid, ref evt);

            if (!evt.Stop)
            {
                var threshold = heartrate.StrainDamageThresholds.HighestMatch(Strain((uid, heartrate)));
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

            var overlays = new PotentiallyUpdateDamageOverlayEvent(uid);
            RaiseLocalEvent(uid, ref overlays, true);
        }
    }

    private void StopHeart(Entity<HeartrateComponent> ent)
    {
        ent.Comp.Running = false;
        Dirty(ent);

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
        Dirty(ent);

        var stoppedEvt = new HeartStoppedEvent();
        RaiseLocalEvent(ent, ref stoppedEvt);

        _statusEffects.TryUpdateStatusEffectDuration(ent, ent.Comp.HeartStoppedEffect, out _);
    }

    private void OnApplyMetabolicMultiplier(Entity<HeartrateComponent> ent, ref ApplyMetabolicMultiplierEvent args)
    {
        ent.Comp.UpdateIntervalMultiplier = args.Multiplier;
        Dirty(ent);
    }

    private void OnHeartBeatStrain(Entity<HeartStopOnHighStrainComponent> ent, ref HeartBeatEvent args)
    {
        var seed = SharedRandomExtensions.HashCodeCombine(new() { (int)_timing.CurTick.Value, GetNetEntity(ent).Id });
        var rand = new System.Random(seed);

        if (_statusEffects.HasEffectComp<PreventHeartStopFromStrainStatusEffectComponent>(ent))
            return;

        var strain = Strain((ent.Owner, Comp<HeartrateComponent>(ent)));
        args.Stop = args.Stop || rand.Prob(ent.Comp.Chance) && strain > ent.Comp.Threshold;
    }

    private void OnHeartBeatStrainMessage(Entity<HeartStopOnHighStrainComponent> ent, ref BeforeTargetDefibrillatedEvent args)
    {
        if (_statusEffects.HasEffectComp<PreventHeartStopFromStrainStatusEffectComponent>(ent))
            return;

        var heartrate = Comp<HeartrateComponent>(ent);

        var volume = ComputeBloodVolume((ent.Owner, heartrate));
        var tone = ComputeVascularTone((ent.Owner, heartrate));
        var perfusion = MathF.Min(volume, tone);
        var function = ComputeLungFunction((ent.Owner, heartrate));
        var supply = function * perfusion;
        var demand = ComputeMetabolicRate((ent.Owner, heartrate));
        var compensation = ComputeCompensation((ent.Owner, heartrate), supply, demand);
        var strain = heartrate.CompensationStrainCoefficient * compensation + heartrate.CompensationStrainConstant;

        if (strain < ent.Comp.Threshold)
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

    private void RecomputeVitals(Entity<HeartrateComponent> ent)
    {
        var volume = ComputeBloodVolume(ent);
        var tone = ComputeVascularTone(ent);

        var perfusion = MathF.Min(volume, MathF.Min(tone, CardiacOutput(ent)));

        var function = ComputeLungFunction(ent);

        var supply = function * perfusion;

        var demand = ComputeMetabolicRate(ent);

        var compensation = ComputeCompensation(ent, supply, demand);

        perfusion *= compensation;
        supply = function * perfusion;

        ent.Comp.Perfusion = perfusion;
        ent.Comp.Compensation = compensation;
        ent.Comp.OxygenSupply = supply;
        ent.Comp.OxygenDemand = demand;

        Dirty(ent);
    }

    [Access(typeof(HeartSystem), typeof(HeartrateComponent))]
    public float CardiacOutput(Entity<HeartrateComponent> ent)
    {
        var baseEv = new BaseCardiacOutputEvent(!ent.Comp.Running ? 0f : 1f - (ent.Comp.Damage.Float() / ent.Comp.MaxDamage.Float()));
        RaiseLocalEvent(ent, ref baseEv);

        var modifiedEv = new ModifiedCardiacOutputEvent(baseEv.Output);
        RaiseLocalEvent(ent, ref modifiedEv);

        return Math.Max(modifiedEv.Output, ent.Comp.MinimumCardiacOutput);
    }

    [Access(typeof(HeartSystem), typeof(HeartrateComponent))]
    public float ComputeCompensation(Entity<HeartrateComponent> ent, float supply, float demand)
    {
        var invert = MathF.Log(demand / supply);
        if (!float.IsFinite(invert))
            throw new InvalidOperationException($"demand/supply {demand}/{supply} is not finite: {invert}");

        var targetCompensation = ent.Comp.CompensationCoefficient * invert + ent.Comp.CompensationConstant;
        var healthFactor = !ent.Comp.Running ? 0f : 1f - (ent.Comp.Damage.Float() / ent.Comp.MaxDamage.Float());

        return Math.Max(targetCompensation * healthFactor, 1f);
    }

    [Access(typeof(HeartSystem), typeof(HeartrateComponent))]
    public float ComputeBloodVolume(Entity<HeartrateComponent> ent)
    {
        var bloodstream = Comp<BloodstreamComponent>(ent);
        if (!_solutionContainer.ResolveSolution(ent.Owner, bloodstream.BloodSolutionName,
            ref bloodstream.BloodSolution, out var bloodSolution))
        {
            return 1f;
        }

        return Math.Max(bloodSolution.Volume.Float() / bloodSolution.MaxVolume.Float(), ent.Comp.MinimumBloodVolume);
    }

    [Access(typeof(HeartSystem), typeof(HeartrateComponent))]
    public float ComputeVascularTone(Entity<HeartrateComponent> ent)
    {
        var baseEv = new BaseVascularToneEvent(1f);
        RaiseLocalEvent(ent, ref baseEv);

        var modifiedEv = new ModifiedVascularToneEvent(baseEv.Tone);
        RaiseLocalEvent(ent, ref modifiedEv);

        return Math.Max(modifiedEv.Tone, ent.Comp.MinimumVascularTone);
    }

    [Access(typeof(HeartSystem), typeof(HeartrateComponent))]
    public float ComputeMetabolicRate(Entity<HeartrateComponent> ent)
    {
        var baseEv = new BaseMetabolicRateEvent(1f);
        RaiseLocalEvent(ent, ref baseEv);

        var modifiedEv = new ModifiedMetabolicRateEvent(baseEv.Rate);
        RaiseLocalEvent(ent, ref modifiedEv);

        return modifiedEv.Rate;
    }

    [Access(typeof(HeartSystem), typeof(HeartrateComponent))]
    public float ComputeLungFunction(Entity<HeartrateComponent> ent)
    {
        var baseEv = new BaseLungFunctionEvent(1f);
        RaiseLocalEvent(ent, ref baseEv);

        var modifiedEv = new ModifiedLungFunctionEvent(baseEv.Function);
        RaiseLocalEvent(ent, ref modifiedEv);

        return Math.Max(modifiedEv.Function, ent.Comp.MinimumLungFunction);
    }

    private float OxygenBalance(Entity<HeartrateComponent> ent)
    {
        return ent.Comp.OxygenSupply / ent.Comp.OxygenDemand;
    }

    public float Strain(Entity<HeartrateComponent> ent)
    {
        return Math.Max(ent.Comp.CompensationStrainCoefficient * ent.Comp.Compensation + ent.Comp.CompensationStrainConstant, 0f);
    }

    public int HeartRate(Entity<HeartrateComponent> ent)
    {
        if (!ent.Comp.Running)
            return 0;

        var seed = SharedRandomExtensions.HashCodeCombine(new() { (int)_timing.CurTick.Value, GetNetEntity(ent).Id });
        var rand = new System.Random(seed);

        var deviation = rand.Next(-ent.Comp.HeartRateDeviation, ent.Comp.HeartRateDeviation);

        return Math.Max((int)MathHelper.Lerp(ent.Comp.HeartRateFullPerfusion, ent.Comp.HeartRateNoPerfusion, Strain(ent)) + deviation, 0);
    }

    public (int, int) BloodPressure(Entity<HeartrateComponent> ent)
    {
        var seed = SharedRandomExtensions.HashCodeCombine(new() { (int)_timing.CurTick.Value, GetNetEntity(ent).Id });
        var rand = new System.Random(seed);

        var deviationA = rand.Next(-ent.Comp.BloodPressureDeviation, ent.Comp.BloodPressureDeviation);
        var deviationB = rand.Next(-ent.Comp.BloodPressureDeviation, ent.Comp.BloodPressureDeviation);

        var upper = (int)Math.Max((ent.Comp.SystolicBase * ent.Comp.Perfusion + deviationA), 0);
        var lower = (int)Math.Max((ent.Comp.DiastolicBase * ent.Comp.Perfusion + deviationB), 0);

        return (upper, lower);
    }

    public int Etco2(Entity<HeartrateComponent> ent)
    {
        var seed = SharedRandomExtensions.HashCodeCombine(new() { (int)_timing.CurTick.Value, GetNetEntity(ent).Id });
        var rand = new System.Random(seed);

        var deviation = rand.Next(-ent.Comp.Etco2Deviation, ent.Comp.Etco2Deviation);

        var baseEtco2 = ent.Comp.Etco2Base * ComputeExhaleEfficiencyModifier(ent);

        return Math.Max((int)baseEtco2 + deviation, 0);
    }

    public float ComputeExhaleEfficiencyModifier(Entity<HeartrateComponent> ent)
    {
        return Math.Max(ent.Comp.Perfusion, ent.Comp.MinimumPerfusionEtco2Modifier) * ComputeRespiratoryRateModifier(ent);
    }

    public float ComputeRespiratoryRateModifier(Entity<HeartrateComponent> ent)
    {
        var balance = ent.Comp.OxygenSupply / ent.Comp.OxygenDemand;
        var rate = Math.Max(1f/(ent.Comp.RespiratoryRateCoefficient * balance) + ent.Comp.RespiratoryRateConstant, ent.Comp.MinimumRespiratoryRateModifier);

        var modifiedEv = new ModifiedRespiratoryRateEvent(rate);
        RaiseLocalEvent(ent, ref modifiedEv);

        return modifiedEv.Rate;
    }

    public int RespiratoryRate(Entity<HeartrateComponent> ent)
    {
        var breathDuration = ent.Comp.RespiratoryRateNormalBreath * ComputeRespiratoryRateModifier(ent);
        if (breathDuration <= 0f)
            return 0;

        return (int)(60f / breathDuration);
    }

    public FixedPoint2 Spo2(Entity<HeartrateComponent> ent)
    {
        return FixedPoint2.Clamp(OxygenBalance(ent), 0, 1);
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
