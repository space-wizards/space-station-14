using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Shared.Construction
{
    [DataDefinition]
    public class ComponentConstructionGraphStep : ArbitraryInsertConstructionGraphStep
    {
        [DataField("component")] public string Component { get; private set; } = string.Empty;

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
                ? Loc.GetString("Next, insert an entity with a {0} component.", Component) // Terrible.
                : Loc.GetString("Next, insert {0}", Name));
        }
    }
}
