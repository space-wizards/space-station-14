using Content.Server.Ame.Components;
using Content.Shared.Conditions;
using Content.Shared.Conditions.Satisfier;
using Content.Shared.Conditions.UnifiedConditions;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Content.Shared.Examine;

namespace Content.Server.Construction.Conditions;

[UsedImplicitly]
[DataDefinition]
public sealed partial class AmeShieldIntegrity : GraphConditionBase<IAmeShieldIntegrityCondition>, IAmeShieldIntegrityCondition, IConditionWithDefaultSatisfactionRule
{

    [DataField]
    public float IntegrityThreshold { get; set; } = 80;

    /// <summary>
    /// If true, checks for the integrity being above the threshold.
    /// if false, checks for it being below.
    /// </summary>
    [DataField]
    public bool CheckAbove = true;

    public bool Condition(EntityUid uid, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<AmeShieldComponent>(uid, out var shield))
            return true;

        if (CheckAbove)
        {
            return shield.CoreIntegrity >= IntegrityThreshold;
        }
        return shield.CoreIntegrity < IntegrityThreshold;
    }

    public override bool DoExamine(ExaminedEvent args)
    {
        return false;
    }

    public override IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
    {
        yield return new ConstructionGuideEntry();
    }

    public WithThreshold.Comparator Comparator => CheckAbove?WithThreshold.Comparator.GreaterEqual:WithThreshold.Comparator.Less;
}
