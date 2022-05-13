using Content.Shared.Examine;

namespace Content.Shared.Construction
{
    [ImplicitDataDefinitionForInheritors]
    public interface IGraphCondition
    {
        bool Condition(EntityUid uid, IEntityManager entityManager);
        bool DoExamine(ExaminedEvent args);
        IEnumerable<ConstructionGuideEntry> GenerateGuideEntry();
    }
}
