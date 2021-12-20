using Content.Shared.Examine;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Shared.Construction.Steps
{
    public abstract class ArbitraryInsertConstructionGraphStep : EntityInsertConstructionGraphStep
    {
        [DataField("name")] public string Name { get; private set; } = string.Empty;

        [DataField("icon")] public SpriteSpecifier? Icon { get; private set; } = null;

        public override void DoExamine(ExaminedEvent examinedEvent)
        {
            if (string.IsNullOrEmpty(Name))
                return;

            examinedEvent.Message.AddMarkup(Loc.GetString("construction-insert-arbitrary-entity", ("stepName", Name)));
        }

        public override ConstructionGuideEntry GenerateGuideEntry()
        {
            return new ConstructionGuideEntry()
            {
                Localization = "construction-presenter-arbitrary-step",
                Arguments = new (string, object)[]{("name", Name)},
                Icon = Icon,
            };
        }
    }
}
