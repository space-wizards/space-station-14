using Content.Shared.Examine;

namespace Content.Shared.Construction.Steps
{
    [DataDefinition]
    public sealed class ComponentConstructionGraphStep : ArbitraryInsertConstructionGraphStep
    {
        [DataField("component")] public string Component { get; } = string.Empty;

        public override bool EntityValid(EntityUid uid, IEntityManager entityManager, IComponentFactory compFactory)
        {
            foreach (var component in entityManager.GetComponents(uid))
            {
                if (compFactory.GetComponentName(component.GetType()) == Component)
                    return true;
            }

            return false;
        }

        public override void DoExamine(ExaminedEvent examinedEvent)
        {
            examinedEvent.Message.AddMarkup(string.IsNullOrEmpty(Name)
                ? Loc.GetString(
                    "construction-insert-entity-with-component",
                    ("componentName", Component))// Terrible.
                : Loc.GetString(
                    "construction-insert-exact-entity",
                    ("entityName", Name)));
        }
    }
}
