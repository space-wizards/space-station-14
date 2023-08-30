namespace Content.Shared.Construction.Steps
{
    [ImplicitDataDefinitionForInheritors]
    public abstract partial class EntityInsertConstructionGraphStep : ConstructionGraphStep
    {
        [DataField("store")] public string Store { get; private set; } = string.Empty;

        public abstract bool EntityValid(EntityUid uid, IEntityManager entityManager, IComponentFactory compFactory);
    }
}
