#nullable enable
using Content.Shared.GameObjects.Components.Tag;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Construction
{
    [DataDefinition]
    public class TagConstructionGraphStep : ArbitraryInsertConstructionGraphStep
    {
        [DataField("tag")]
        private string? _tag = null;

        public override bool EntityValid(IEntity entity)
        {
            return !string.IsNullOrEmpty(_tag) && entity.HasTag(_tag);
        }
    }
}
