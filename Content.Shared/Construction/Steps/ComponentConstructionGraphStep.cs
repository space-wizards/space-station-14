#nullable enable
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Shared.Construction.Steps
{
    [DataDefinition]
    public class ComponentConstructionGraphStep : ArbitraryInsertConstructionGraphStep
    {
        [DataField("component")] public string Component { get; } = string.Empty;

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
                ? Robust.Shared.Localization.Loc.GetString(
                    "construction-insert-entity-with-component",
                    ("componentName", Component))// Terrible.
                : Robust.Shared.Localization.Loc.GetString(
                    "construction-insert-exact-entity",
                    ("entityName", Name)));
        }
    }
}
