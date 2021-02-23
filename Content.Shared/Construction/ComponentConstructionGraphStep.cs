using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Construction
{
    public class ComponentConstructionGraphStep : ArbitraryInsertConstructionGraphStep
    {
        public string Component { get; private set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => x.Component, "component", string.Empty);

        }

        public override bool EntityValid(IEntity entity)
        {
            foreach (var component in entity.GetAllComponents())
            {
                if (component.Name == Component)
                    return true;
            }

            return false;
        }

        public override void DoExamine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddMarkup(string.IsNullOrEmpty(Name)
                ? Loc.GetString(
                    "construction-insert-entity-with-component",
                    ("componentName", Component))// Terrible.
                : Loc.GetString(
                    "construction-insert-exact-entity",
                    ("entityName", Name)));
        }
    }
}
