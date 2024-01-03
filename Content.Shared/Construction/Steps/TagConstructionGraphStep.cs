using Content.Shared.Examine;
using Content.Shared.Tag;

namespace Content.Shared.Construction.Steps
{
    [DataDefinition]
    public sealed partial class TagConstructionGraphStep : ArbitraryInsertConstructionGraphStep
    {
        [DataField("tag")]
        private string _tag = string.Empty;

        public override bool EntityValid(EntityUid uid, IEntityManager entityManager, IComponentFactory compFactory)
        {
            var tagSystem = entityManager.EntitySysManager.GetEntitySystem<TagSystem>();
            return !string.IsNullOrEmpty(_tag) && tagSystem.HasTag(uid, _tag);
        }

        public override void DoExamine(ExaminedEvent examinedEvent)
        {
            if (string.IsNullOrEmpty(Name))
                return;

            examinedEvent.Message.AddMarkup(Loc.GetString("construction-insert-arbitrary-entity",
                ("name", GetLocName())));
        }

        public override ConstructionGuideEntry GenerateGuideEntry()
        {
            return new ConstructionGuideEntry
            {
                Localization = "construction-presenter-arbitrary-step",
                Arguments = new (string, object)[] { ("name", GetLocName()) },
                Icon = Icon,
            };
        }

        private string GetLocName()
        {
            return Loc.TryGetString($"construction-insert-tag-{_tag}", out var locName) ? locName : Name;
        }
    }
}
