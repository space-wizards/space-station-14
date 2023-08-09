namespace Content.Shared.Construction.Steps
{
    [ImplicitDataDefinitionForInheritors]
    public abstract class EntityInsertConstructionGraphStep : ConstructionGraphStep
    {
        [DataField("store")] public string Store { get; } = string.Empty;

        public abstract bool EntityValid(EntityUid uid, IEntityManager entityManager, IComponentFactory compFactory);
    }
}
