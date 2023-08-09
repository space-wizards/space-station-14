using Content.Shared.Tag;

namespace Content.Shared.Construction.Steps
{
    [DataDefinition]
    public sealed class TagConstructionGraphStep : ArbitraryInsertConstructionGraphStep
    {
        [DataField("tag")]
        private string? _tag;

        public override bool EntityValid(EntityUid uid, IEntityManager entityManager, IComponentFactory compFactory)
        {
            var tagSystem = entityManager.EntitySysManager.GetEntitySystem<TagSystem>();
            return !string.IsNullOrEmpty(_tag) && tagSystem.HasTag(uid, _tag);
        }
    }
}
