using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Offbrand.Wounds;

[DataDefinition, Serializable, NetSerializable]
public sealed partial class WoundableHealthAnalyzerData
{
    [DataField]
    public double BrainHealth;

    [DataField]
    public AttributeRating BrainHealthRating;

    [DataField]
    public double HeartHealth;

    [DataField]
    public AttributeRating HeartHealthRating;

    [DataField]
    public (int, int) BloodPressure;

    [DataField]
    public AttributeRating BloodPressureRating;

    [DataField]
    public double BloodOxygenation;

    [DataField]
    public AttributeRating BloodOxygenationRating;

    [DataField]
    public double BloodFlow;

    [DataField]
    public AttributeRating BloodFlowRating;

    [DataField]
    public int HeartRate;

    [DataField]
    public AttributeRating HeartRateRating;

    [DataField]
    public double LungHealth;

    [DataField]
    public AttributeRating LungHealthRating;

    [DataField]
    public bool AnyVitalCritical;

    [DataField]
    public List<string>? Wounds;

    [DataField]
    public Dictionary<ProtoId<ReagentPrototype>, (FixedPoint2 InBloodstream, FixedPoint2 Metabolites)>? Reagents;

    [DataField]
    public bool NonMedicalReagents;
}

[Serializable, NetSerializable]
public enum AttributeRating : byte
{
    Good = 0,
    Okay = 1,
    Poor = 2,
    Bad = 3,
    Awful = 4,
    Dangerous = 5,
}

public abstract class SharedWoundableHealthAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly BrainDamageSystem _brainDamage = default!;
    [Dependency] private readonly HeartSystem _heart = default!;
    [Dependency] private readonly ShockThresholdsSystem _shockThresholds = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    protected const string MedicineGroup = "Medicine";

    private AttributeRating RateHigherIsBetter(double value)
    {
        return RateHigherIsWorse(1d - value);
    }

    private AttributeRating RateHigherIsWorse(double value)
    {
        return (AttributeRating)(byte)Math.Clamp(Math.Floor(6d * value), 0d, 5d);
    }

    public List<string>? SampleWounds(EntityUid uid)
    {
        if (!_statusEffects.TryEffectsWithComp<AnalyzableWoundComponent>(uid, out var wounds))
            return null;

        var described = new List<string>();

        foreach (var analyzable in wounds)
        {
            var wound = Comp<WoundComponent>(analyzable);
            var damage = wound.Damage;

            if (analyzable.Comp1.Descriptions.HighestMatch(damage.GetTotal()) is { } message)
                described.Add(message);
        }

        return described;
    }

    public virtual Dictionary<ProtoId<ReagentPrototype>, (FixedPoint2 InBloodstream, FixedPoint2 Metabolites)>? SampleReagents(EntityUid uid, out bool hasNonMedical)
    {
        hasNonMedical = false;
        return null;
    }

    public WoundableHealthAnalyzerData? TakeSample(EntityUid uid, bool withWounds = true)
    {
        if (!HasComp<WoundableComponent>(uid))
            return null;

        if (!TryComp<HeartrateComponent>(uid, out var heartrate))
            return null;

        if (!TryComp<BrainDamageComponent>(uid, out var brainDamage))
            return null;

        if (!TryComp<LungDamageComponent>(uid, out var lungDamage))
            return null;

        var brainHealth = 1d - ((double)brainDamage.Damage / (double)brainDamage.MaxDamage);
        var heartHealth = 1d - ((double)heartrate.Damage / (double)heartrate.MaxDamage);
        var lungHealth = 1d - ((double)lungDamage.Damage / (double)lungDamage.MaxDamage);
        var strain = _heart.HeartStrain((uid, heartrate)).Double() / 4d;
        var (upper, lower) = _heart.BloodPressure((uid, heartrate));
        var oxygenation = _heart.BloodOxygenation((uid, heartrate)).Double();
        var circulation = _heart.BloodCirculation((uid, heartrate)).Double();
        var flow = _heart.BloodFlow((uid, heartrate)).Double();

        var hasNonMedical = false;
        var reagents = withWounds ? SampleReagents(uid, out hasNonMedical) : null;

        return new WoundableHealthAnalyzerData()
            {
                BrainHealth = brainHealth,
                BrainHealthRating = RateHigherIsBetter(brainHealth),
                HeartHealth = heartHealth,
                HeartHealthRating = RateHigherIsBetter(heartHealth),
                BloodPressure = (upper.Int(), lower.Int()),
                BloodPressureRating = RateHigherIsBetter(circulation),
                BloodOxygenation = oxygenation,
                BloodOxygenationRating = RateHigherIsBetter(oxygenation),
                BloodFlow = flow,
                BloodFlowRating = RateHigherIsBetter(flow),
                HeartRate = _heart.HeartRate((uid, heartrate)).Int(),
                HeartRateRating = !heartrate.Running ? AttributeRating.Dangerous : RateHigherIsWorse(strain),
                LungHealth = lungHealth,
                LungHealthRating = RateHigherIsBetter(lungHealth),
                AnyVitalCritical = _shockThresholds.IsCritical(uid) || _brainDamage.IsCritical(uid) || _heart.IsCritical(uid),
                Wounds = withWounds ? SampleWounds(uid) : null,
                Reagents = reagents,
                NonMedicalReagents = hasNonMedical,
            };
    }
}
