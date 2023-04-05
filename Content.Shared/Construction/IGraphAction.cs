namespace Content.Shared.Construction
{
    [ImplicitDataDefinitionForInheritors]
    public interface IGraphAction
    {
        void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager);
    }
}
