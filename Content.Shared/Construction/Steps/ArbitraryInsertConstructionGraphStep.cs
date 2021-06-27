#nullable enable
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Shared.Construction.Steps
{
    public abstract class ArbitraryInsertConstructionGraphStep : EntityInsertConstructionGraphStep
    {
        [DataField("name")] public string Name { get; private set; } = string.Empty;

        [DataField("icon")] public SpriteSpecifier Icon { get; private set; } = SpriteSpecifier.Invalid;

        public override void DoExamine(FormattedMessage message, bool inDetailsRange)
        {
            if (string.IsNullOrEmpty(Name)) return;
            message.AddMarkup(Loc.GetString("construction-insert-arbitrary-entity", ("stepName", Name)));
        }
    }
}
