using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Random.Helpers;
using Robust.Shared.Timing;

namespace Content.Shared._Offbrand.Wounds;

public sealed partial class PerfusionSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PerfusionComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PerfusionComponent, ApplyMetabolicMultiplierEvent>(OnApplyMetabolicMultiplier);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PerfusionComponent>();
        while (query.MoveNext(out var uid, out var perfusion))
        {
            if (perfusion.LastUpdate is not { } last || last + perfusion.AdjustedUpdateInterval >= _timing.CurTime)
                continue;

            var delta = _timing.CurTime - last;
            perfusion.LastUpdate = _timing.CurTime;
            Dirty(uid, perfusion);

            RecomputeVitals((uid, perfusion));

            var strainChanged = new AfterStrainChangedEvent();
            RaiseLocalEvent(uid, ref strainChanged);

            var respiration = new ApplyRespiratoryRateModifiersEvent(ComputeRespiratoryRateModifier((uid, perfusion)));
            RaiseLocalEvent(uid, ref respiration);

            var evt = new HeartBeatEvent();
            RaiseLocalEvent(uid, ref evt);

        }
    }

    private void OnMapInit(Entity<PerfusionComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.LastUpdate = _timing.CurTime;
    }

    private void OnApplyMetabolicMultiplier(Entity<PerfusionComponent> ent, ref ApplyMetabolicMultiplierEvent args)
    {
        ent.Comp.UpdateIntervalMultiplier = args.Multiplier;
        Dirty(ent);
    }

    [Access(typeof(PerfusionSystem), typeof(PerfusionComponent))]
    public float ComputeBloodVolume(Entity<PerfusionComponent> ent)
    {
        var bloodstream = Comp<BloodstreamComponent>(ent);
        if (!_solutionContainer.ResolveSolution(ent.Owner, bloodstream.BloodSolutionName,
                ref bloodstream.BloodSolution, out var bloodSolution))
        {
            return 1f;
        }
        var bloodReferenceSolution = bloodstream.BloodReferenceSolution;

        return Math.Max(bloodSolution.Volume.Float() / bloodReferenceSolution.Volume.Float(), ent.Comp.MinimumBloodVolume);
    }

    [Access(typeof(PerfusionSystem), typeof(PerfusionComponent))]
    public float ComputeVascularTone(Entity<PerfusionComponent> ent)
    {
        var baseEv = new BaseVascularToneEvent(0f);
        RaiseLocalEvent(ent, ref baseEv);

        var modifiedEv = new ModifiedVascularToneEvent(baseEv.Tone);
        RaiseLocalEvent(ent, ref modifiedEv);

        return Math.Max(modifiedEv.Tone, ent.Comp.MinimumVascularTone);
    }

    [Access(typeof(PerfusionSystem), typeof(PerfusionComponent))]
    public (float? Base, float Actual) CardiacOutput(Entity<PerfusionComponent> ent)
    {
        var baseEv = new BaseCardiacOutputEvent(0f);
        RaiseLocalEvent(ent, ref baseEv);

        var modifiedEv = new ModifiedCardiacOutputEvent(baseEv.Output ?? 0f);
        RaiseLocalEvent(ent, ref modifiedEv);

        return (baseEv.Output, Math.Max(modifiedEv.Output, ent.Comp.MinimumCardiacOutput));
    }

    [Access(typeof(PerfusionSystem), typeof(PerfusionComponent))]
    public (float Compensation, float Strain) ComputeCompensation(Entity<PerfusionComponent> ent, float supply, float demand)
    {
        var evt = new CardiacCompensationEvent(0f, 0f, supply, demand);
        RaiseLocalEvent(ent, ref evt);

        return (evt.Compensation, evt.Strain);
    }

    [Access(typeof(PerfusionSystem), typeof(PerfusionComponent))]
    public float ComputeMetabolicRate(Entity<PerfusionComponent> ent)
    {
        var baseEv = new BaseMetabolicRateEvent(1f);
        RaiseLocalEvent(ent, ref baseEv);

        var modifiedEv = new ModifiedMetabolicRateEvent(baseEv.Rate);
        RaiseLocalEvent(ent, ref modifiedEv);

        return modifiedEv.Rate;
    }

    [Access(typeof(PerfusionSystem), typeof(PerfusionComponent))]
    public float ComputeLungFunction(Entity<PerfusionComponent> ent)
    {
        var baseEv = new BaseLungFunctionEvent(0f);
        RaiseLocalEvent(ent, ref baseEv);

        var modifiedEv = new ModifiedLungFunctionEvent(baseEv.Function);
        RaiseLocalEvent(ent, ref modifiedEv);

        return Math.Max(modifiedEv.Function, ent.Comp.MinimumLungFunction);
    }

    private void RecomputeVitals(Entity<PerfusionComponent> ent)
    {
        var volume = ComputeBloodVolume(ent);
        var tone = ComputeVascularTone(ent);

        var (baseOutput, actualOutput) = CardiacOutput(ent);
        var perfusion = MathF.Min(volume, MathF.Min(tone, actualOutput));

        var function = ComputeLungFunction(ent);

        var supply = function * perfusion;

        var demand = ComputeMetabolicRate(ent);

        var compensation = ComputeCompensation(ent, supply, demand);

        perfusion *= compensation.Compensation;
        supply = function * perfusion;

        ent.Comp.Perfusion = perfusion;
        ent.Comp.BaseCardiacOutput = baseOutput;
        ent.Comp.Strain = compensation.Strain;
        ent.Comp.OxygenSupply = supply;
        ent.Comp.OxygenDemand = demand;

        Dirty(ent);
    }

    private float OxygenBalance(Entity<PerfusionComponent> ent)
    {
        return ent.Comp.OxygenSupply / ent.Comp.OxygenDemand;
    }

    public int HeartRate(Entity<PerfusionComponent> ent)
    {
        if (ent.Comp.BaseCardiacOutput is null)
            return 0;

        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id);
        var rand = new System.Random(seed);

        var deviation = rand.Next(-ent.Comp.HeartRateDeviation, ent.Comp.HeartRateDeviation);

        return Math.Max((int)MathHelper.Lerp(ent.Comp.HeartRateFullPerfusion, ent.Comp.HeartRateNoPerfusion, ent.Comp.Strain) + deviation, 0);
    }

    public (int, int) BloodPressure(Entity<PerfusionComponent> ent)
    {
        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id);
        var rand = new System.Random(seed);

        var deviationA = rand.Next(-ent.Comp.BloodPressureDeviation, ent.Comp.BloodPressureDeviation);
        var deviationB = rand.Next(-ent.Comp.BloodPressureDeviation, ent.Comp.BloodPressureDeviation);

        var upper = (int)Math.Max((ent.Comp.SystolicBase * ent.Comp.Perfusion + deviationA), 0);
        var lower = (int)Math.Max((ent.Comp.DiastolicBase * ent.Comp.Perfusion + deviationB), 0);

        return (upper, lower);
    }

    public int Etco2(Entity<PerfusionComponent> ent)
    {
        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id);
        var rand = new System.Random(seed);

        var deviation = rand.Next(-ent.Comp.Etco2Deviation, ent.Comp.Etco2Deviation);

        var baseEtco2 = ent.Comp.Etco2Base * ComputeExhaleEfficiencyModifier(ent);

        return Math.Max((int)baseEtco2 + deviation, 0);
    }

    public float ComputeExhaleEfficiencyModifier(Entity<PerfusionComponent> ent)
    {
        return Math.Max(ent.Comp.Perfusion, ent.Comp.MinimumPerfusionEtco2Modifier) * ComputeRespiratoryRateModifier(ent);
    }

    public float ComputeRespiratoryRateModifier(Entity<PerfusionComponent> ent)
    {
        var balance = ent.Comp.OxygenSupply / ent.Comp.OxygenDemand;
        var rate = Math.Max(1f/(ent.Comp.RespiratoryRateCoefficient * balance) + ent.Comp.RespiratoryRateConstant, ent.Comp.MinimumRespiratoryRateModifier);

        var modifiedEv = new ModifiedRespiratoryRateEvent(rate);
        RaiseLocalEvent(ent, ref modifiedEv);

        return modifiedEv.Rate;
    }

    public int RespiratoryRate(Entity<PerfusionComponent> ent)
    {
        var breathDuration = ent.Comp.RespiratoryRateNormalBreath * ComputeRespiratoryRateModifier(ent);
        if (breathDuration <= 0f)
            return 0;

        return (int)(60f / breathDuration);
    }

    public FixedPoint2 Spo2(Entity<PerfusionComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return FixedPoint2.Zero;

        return FixedPoint2.Clamp(OxygenBalance((ent, ent.Comp)), 0, 1);
    }
}
