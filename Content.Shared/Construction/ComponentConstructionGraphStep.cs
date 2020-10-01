using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Construction
{
    public class ComponentConstructionGraphStep : EntityInsertConstructionGraphStep
    {
        public string Component { get; private set; }
        public string Name { get; private set; }
        public SpriteSpecifier Icon { get; private set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => x.Component, "component", string.Empty);
            serializer.DataField(this, x => x.Name, "name", string.Empty);
            serializer.DataField(this, x => x.Icon, "icon", null);
        }

        public override void DoExamine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddMarkup(string.IsNullOrEmpty(Name)
                ? Loc.GetString("Next, insert an entity with a {0} component.", Component) // Terrible.
                : Loc.GetString("Next, insert {0}", Name));
        }
    }
}
