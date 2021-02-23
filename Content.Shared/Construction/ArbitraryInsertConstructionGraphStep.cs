using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Construction
{
    public abstract class ArbitraryInsertConstructionGraphStep : EntityInsertConstructionGraphStep
    {
        public string Name { get; private set; }
        public SpriteSpecifier Icon { get; private set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => x.Icon, "icon", SpriteSpecifier.Invalid);
            serializer.DataField(this, x => x.Name, "name", string.Empty);
        }

        public override void DoExamine(FormattedMessage message, bool inDetailsRange)
        {
            if (string.IsNullOrEmpty(Name)) return;
            message.AddMarkup(Loc.GetString("construction-insert-arbitrary-entity", ("stepName", Name)));
        }
    }
}
