using Content.Shared.Tag;

namespace Content.Shared.Construction.Steps
{
    [DataDefinition]
    public sealed class TagConstructionGraphStep : ArbitraryInsertConstructionGraphStep
    {
        [DataField("tag")]
        private string? _tag = null;

        public override bool EntityValid(EntityUid uid, IEntityManager entityManager)
        {
            var tagSystem = EntitySystem.Get<TagSystem>();
            return !string.IsNullOrEmpty(_tag) && tagSystem.HasTag(uid, _tag);
        }
    }
}
