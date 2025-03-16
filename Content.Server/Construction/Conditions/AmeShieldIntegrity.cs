using Content.Server.Ame.Components;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Content.Shared.Examine;

namespace Content.Server.Construction.Conditions;

[UsedImplicitly]
[DataDefinition]
public sealed partial class AmeShieldIntegrity : IGraphCondition
{
    [DataField]
    public float IntegrityThreshold = 80;

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

    public bool DoExamine(ExaminedEvent args)
    {
        return false;
    }

    public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
    {
        yield return new ConstructionGuideEntry();
    }
}
