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
    [Dependency] private readonly PerfusionSystem _perfusion = default!;
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

    public MetricRanking Ranking(Entity<PerfusionComponent> ent)
    {
        var strain = (MetricRanking)Math.Min((int)MathF.Round(4f * ent.Comp.Strain), 4);
        var spo2 = (MetricRanking)Math.Min((int)MathF.Round(4f * (1f - _perfusion.Spo2(ent.AsNullable()).Float())), 4);

        if ((byte)spo2 > (byte)strain)
            return spo2;

        return strain;
    }

    public WoundableHealthAnalyzerData? TakeSample(EntityUid uid, bool withWounds = true)
    {
        if (!HasComp<WoundableComponent>(uid))
            return null;

        if (!TryComp<PerfusionComponent>(uid, out var heartrate))
            return null;

        if (!TryComp<BrainDamageThresholdsComponent>(uid, out var brainDamageThresholds))
            return null;

        var (upper, lower) = _perfusion.BloodPressure((uid, heartrate));

        var hasNonMedical = false;
        var reagents = withWounds ? SampleReagents(uid, out hasNonMedical) : null;

        return new WoundableHealthAnalyzerData()
            {
                BrainHealth = brainDamageThresholds.DisplayDamage.Float() / brainDamageThresholds.DisplayMaxDamage.Float(),
                BloodPressure = (upper, lower),
                HeartRate = _perfusion.HeartRate((uid, heartrate)),
                Etco2 = _perfusion.Etco2((uid, heartrate)),
                RespiratoryRate = _perfusion.RespiratoryRate((uid, heartrate)),
                Spo2 = _perfusion.Spo2((uid, heartrate)).Float(),
                AnyVitalCritical = _shockThresholds.IsCritical(uid) || brainDamageThresholds.DisplayDamage == 0, // TODO: || _perfusion.IsCritical(uid),
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
