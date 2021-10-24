using Content.Shared.Examine;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Construction
{
    [ImplicitDataDefinitionForInheritors]
    public interface IGraphCondition
    {
        bool Condition(EntityUid uid, IEntityManager entityManager);
        bool DoExamine(ExaminedEvent args) { return false; }
    }
}
