#nullable enable
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Tag;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Construction
{
    public class TagConstructionGraphStep : ArbitraryInsertConstructionGraphStep
    {
        private string? _tag = null;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _tag, "tag", null);
        }

        public override bool EntityValid(IEntity entity)
        {
            return !string.IsNullOrEmpty(_tag) && entity.HasTag(_tag);
        }
    }
}
