using Content.Shared.Tag;

namespace Content.Shared.Construction.Steps
{
    public sealed partial class MultipleTagsConstructionGraphStep : ArbitraryInsertConstructionGraphStep
    {
        [DataField("allTags")]
        private List<string>? _allTags;

        [DataField("anyTags")]
        private List<string>? _anyTags;

        private static bool IsNullOrEmpty<T>(ICollection<T>? list)
        {
            return list == null || list.Count == 0;
        }

        public override bool EntityValid(EntityUid uid, IEntityManager entityManager, IComponentFactory compFactory)
        {
            // This step can only happen if either list has tags.
            if (IsNullOrEmpty(_allTags) && IsNullOrEmpty(_anyTags))
                return false; // Step is somehow invalid, we return.

            // No tags at all.
            if (!entityManager.TryGetComponent(uid, out TagComponent? tags))
                return false;

            var tagSystem = entityManager.EntitySysManager.GetEntitySystem<TagSystem>();

            if (_allTags != null && !tagSystem.HasAllTags(tags, _allTags))
                return false; // We don't have all the tags needed.

            if (_anyTags != null && !tagSystem.HasAnyTag(tags, _anyTags))
                return false; // We don't have any of the tags needed.

            // This entity is valid!
            return true;
        }
    }
}
