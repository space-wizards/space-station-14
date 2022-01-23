using Content.Shared.Tag;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Construction.Steps
{
    [DataDefinition]
    public class TagConstructionGraphStep : ArbitraryInsertConstructionGraphStep
    {
        [DataField("tag")]
        private string? _tag = null;

        public override bool EntityValid(EntityUid uid, IEntityManager entityManager)
        {
            return !string.IsNullOrEmpty(_tag) && entityManager.TryGetComponent(uid, out TagComponent? tags) && tags.HasTag(_tag);
        }
    }
}
