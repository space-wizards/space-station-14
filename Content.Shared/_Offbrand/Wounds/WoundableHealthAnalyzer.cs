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
    public float BrainHealth;

    [DataField]
    public float HeartHealth;

    [DataField]
    public (int, int) BloodPressure;

    [DataField]
    public int HeartRate;

    [DataField]
    public int Etco2;

    [DataField]
    public int RespiratoryRate;

    [DataField]
    public float Spo2;

    [DataField]
    public float LungHealth;

    [DataField]
    public bool AnyVitalCritical;

    [DataField]
    public LocId Etco2Name;

    [DataField]
    public LocId Etco2GasName;

    [DataField]
    public LocId Spo2Name;

    [DataField]
    public LocId Spo2GasName;

    [DataField]
    public List<string>? Wounds;

    [DataField]
    public Dictionary<ProtoId<ReagentPrototype>, (FixedPoint2 InBloodstream, FixedPoint2 Metabolites)>? Reagents;

    [DataField]
    public bool NonMedicalReagents;

    [DataField]
    public MetricRanking Ranking;
}

[Serializable, NetSerializable]
public enum MetricRanking : byte
{
    Good = 0,
    Okay = 1,
    Poor = 2,
    Bad = 3,
    Dangerous = 4,
}

public abstract class SharedWoundableHealthAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly BrainDamageSystem _brainDamage = default!;
    [Dependency] private readonly HeartSystem _heart = default!;
    [Dependency] private readonly ShockThresholdsSystem _shockThresholds = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    protected const string MedicineGroup = "Medicine";

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

    public MetricRanking Ranking(Entity<HeartrateComponent> ent)
    {
        var strain = (MetricRanking)Math.Min((int)MathF.Round(4f * _heart.Strain(ent)), 4);
        var spo2 = (MetricRanking)Math.Min((int)MathF.Round(4f * (1f - _heart.Spo2(ent).Float())), 4);

        if ((byte)spo2 > (byte)strain)
            return spo2;

        return strain;
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

        var brainHealth = 1f - ((float)brainDamage.Damage / (float)brainDamage.MaxDamage);
        var heartHealth = 1f - ((float)heartrate.Damage / (float)heartrate.MaxDamage);
        var lungHealth = 1f - ((float)lungDamage.Damage / (float)lungDamage.MaxDamage);
        var (upper, lower) = _heart.BloodPressure((uid, heartrate));

        var hasNonMedical = false;
        var reagents = withWounds ? SampleReagents(uid, out hasNonMedical) : null;

        return new WoundableHealthAnalyzerData()
            {
                BrainHealth = brainHealth,
                HeartHealth = heartHealth,
                BloodPressure = (upper, lower),
                HeartRate = _heart.HeartRate((uid, heartrate)),
                Etco2 = _heart.Etco2((uid, heartrate)),
                RespiratoryRate = _heart.RespiratoryRate((uid, heartrate)),
                Spo2 = _heart.Spo2((uid, heartrate)).Float(),
                LungHealth = lungHealth,
                AnyVitalCritical = _shockThresholds.IsCritical(uid) || _brainDamage.IsCritical(uid) || _heart.IsCritical(uid),
                Etco2Name = heartrate.Etco2Name,
                Etco2GasName = heartrate.Etco2GasName,
                Spo2Name = heartrate.Spo2Name,
                Spo2GasName = heartrate.Spo2GasName,
                Wounds = withWounds ? SampleWounds(uid) : null,
                Reagents = reagents,
                NonMedicalReagents = hasNonMedical,
                Ranking = Ranking((uid, heartrate)),
            };
    }
}
