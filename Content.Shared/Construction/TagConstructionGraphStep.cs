#nullable enable
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Tag;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Construction
{
    public class TagConstructionGraphStep : ArbitraryInsertConstructionGraphStep
    {
        private List<string>? _tags = null;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _tags, "list", new List<string>());
        }

        public override bool EntityValid(IEntity entity)
        {
            return _tags != null && entity.HasAllTags(_tags);
        }
    }
}
