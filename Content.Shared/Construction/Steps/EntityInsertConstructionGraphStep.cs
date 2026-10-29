namespace Content.Shared.Construction.Steps
{
    [ImplicitDataDefinitionForInheritors]
    public abstract partial class EntityInsertConstructionGraphStep : ConstructionGraphStep
    {
        /// <summary>
        /// A container ID to store the entity in.
        /// </summary>
        [DataField]
        public string Store { get; private set; } = string.Empty;

        /// <summary>
        /// How many of the entity is needed.
        /// </summary>
        [DataField]
        public int Amount { get; private set; } = 1;

        public abstract bool EntityValid(EntityUid uid, IEntityManager entityManager, IComponentFactory compFactory);
    }
}
