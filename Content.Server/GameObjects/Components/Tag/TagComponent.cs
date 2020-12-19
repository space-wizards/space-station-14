using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Tag
{
    [RegisterComponent]
    public class TagComponent : Component
    {
        public override string Name => "Tag";

        private HashSet<string> _tags = new();

        public IReadOnlySet<string> Tags => _tags;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _tags, "tags", new HashSet<string>());
        }

        /// <summary>
        ///     Tries to add a tag if it doesn't already exist.
        /// </summary>
        /// <param name="tag">The tag to add.</param>
        /// <returns>true if it was added, false if it already existed.</returns>
        public bool AddTag(string tag)
        {
            return _tags.Add(tag);
        }

        /// <summary>
        ///     Tries to add the given tags if they don't already exist.
        /// </summary>
        /// <param name="tags">The tags to add.</param>
        /// <returns>true if any tags were added, false if it they all already existed.</returns>
        public bool AddTags(params string[] tags)
        {
            return AddTags(tags.AsEnumerable());
        }

        public bool AddTags(IEnumerable<string> tags)
        {
            var count = _tags.Count;

            _tags.UnionWith(tags);

            return _tags.Count > count;
        }

        /// <summary>
        ///     Checks if a tag has been added.
        /// </summary>
        /// <param name="tag">The tag to check for.</param>
        /// <returns>true if it exists, false otherwise.</returns>
        public bool HasTag(string tag)
        {
            return _tags.Contains(tag);
        }

        /// <summary>
        ///     Checks if all of the given tags have been added.
        /// </summary>
        /// <param name="tags">The tags to check for.</param>
        /// <returns>true if they all exist, false otherwise.</returns>
        public bool HasAllTags(params string[] tags)
        {
            return HasAllTags(tags.AsEnumerable());
        }

        /// <summary>
        ///     Checks if all of the given tags have been added.
        /// </summary>
        /// <param name="tags">The tags to check for.</param>
        /// <returns>true if they all exist, false otherwise.</returns>
        public bool HasAllTags(IEnumerable<string> tags)
        {
            return _tags.IsSupersetOf(tags);
        }

        /// <summary>
        ///     Checks if any of the given tags have been added.
        /// </summary>
        /// <param name="tags">The tags to check for.</param>
        /// <returns>true if any of them exist, false otherwise.</returns>
        public bool HasAnyTag(params string[] tags)
        {
            return HasAnyTag(tags.AsEnumerable());
        }

        /// <summary>
        ///     Checks if any of the given tags have been added.
        /// </summary>
        /// <param name="tags">The tags to check for.</param>
        /// <returns>true if any of them exist, false otherwise.</returns>
        public bool HasAnyTag(IEnumerable<string> tags)
        {
            return _tags.Overlaps(tags);
        }

        /// <summary>
        ///     Tries to remove a tag if it exists.
        /// </summary>
        /// <param name="tag">The tag to remove.</param>
        /// <returns>true if it was removed, false if it didn't exist.</returns>
        public bool RemoveTag(string tag)
        {
            return _tags.Remove(tag);
        }

        /// <summary>
        ///     Tries to remove all of the given tags if they exist.
        /// </summary>
        /// <param name="tags">The tags to remove.</param>
        /// <returns>true if any tag was removed, false otherwise.</returns>
        public bool RemoveTags(params string[] tags)
        {
            return RemoveTags(tags.AsEnumerable());
        }

        /// <summary>
        ///     Tries to remove all of the given tags if they exist.
        /// </summary>
        /// <param name="tags">The tags to remove.</param>
        /// <returns>true if any tag was removed, false otherwise.</returns>
        public bool RemoveTags(IEnumerable<string> tags)
        {
            var count = _tags.Count;

            _tags.ExceptWith(tags);

            return _tags.Count < count;
        }
    }
}
