#nullable enable
using System.Collections;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Tag;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Construction
{
    public class MultipleTagsConstructionGraphStep : ArbitraryInsertConstructionGraphStep
    {
        private List<string>? _allTags = null;
        private List<string>? _anyTags = null;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _allTags, "allTags", null);
            serializer.DataField(ref _anyTags, "anyTags", null);
        }

        private static bool IsNullOrEmpty<T>(ICollection<T>? list)
        {
            return list == null || list.Count == 0;
        }

        public override bool EntityValid(IEntity entity)
        {
            // This step can only happen if either list has tags.
            if (IsNullOrEmpty(_allTags) && IsNullOrEmpty(_anyTags))
                return false; // Step is somehow invalid, we return.

            if (_allTags != null && !entity.HasAllTags(_allTags))
                return false; // We don't have all the tags needed.

            if (_anyTags != null && !entity.HasAnyTag(_anyTags))
                return false; // We don't have any of the tags needed.

            // This entity is valid!
            return true;
        }
    }
}
