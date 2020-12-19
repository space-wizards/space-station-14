using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components.Tag
{
    public static class TagComponentExtensions
    {
        /// <summary>
        ///     Tries to add a tag to an entity if the tag doesn't already exist.
        /// </summary>
        /// <param name="entity">The entity to add the tag to.</param>
        /// <param name="tag">The tag to add.</param>
        /// <returns>
        ///     true if it was added, false otherwise even if it already existed.
        /// </returns>
        public static bool AddTag(this IEntity entity, string tag)
        {
            return entity.EnsureComponent(out TagComponent tagComponent) &&
                   tagComponent.AddTag(tag);
        }

        /// <summary>
        ///     Tries to add the given tags to an entity if the tags don't already exist.
        /// </summary>
        /// <param name="entity">The entity to add the tag to.</param>
        /// <param name="tags">The tags to add.</param>
        /// <returns>
        ///     true if any tags were added, false otherwise even if they all already existed.
        /// </returns>
        public static bool AddTags(this IEntity entity, params string[] tags)
        {
            return entity.EnsureComponent(out TagComponent tagComponent) &&
                   tagComponent.AddTags(tags);
        }

        /// <summary>
        ///     Tries to add the given tags to an entity if the tags don't already exist.
        /// </summary>
        /// <param name="entity">The entity to add the tag to.</param>
        /// <param name="tags">The tags to add.</param>
        /// <returns>
        ///     true if any tags were added, false otherwise even if they all already existed.
        /// </returns>
        public static bool AddTags(this IEntity entity, IEnumerable<string> tags)
        {
            return entity.EnsureComponent(out TagComponent tagComponent) &&
                   tagComponent.AddTags(tags);
        }

        /// <summary>
        ///     Tries to add a tag to an entity if it has a <see cref="TagComponent"/>
        ///     and the tag doesn't already exist.
        /// </summary>
        /// <param name="entity">The entity to add the tag to.</param>
        /// <param name="tag">The tag to add.</param>
        /// <returns>
        ///     true if it was added, false otherwise even if it already existed.
        /// </returns>
        public static bool TryAddTag(this IEntity entity, string tag)
        {
            return entity.TryGetComponent(out TagComponent tagComponent) &&
                   tagComponent.AddTag(tag);
        }

        /// <summary>
        ///     Tries to add the given tags to an entity if it has a
        ///     <see cref="TagComponent"/> and the tags don't already exist.
        /// </summary>
        /// <param name="entity">The entity to add the tag to.</param>
        /// <param name="tags">The tags to add.</param>
        /// <returns>
        ///     true if any tags were added, false otherwise even if they all already existed.
        /// </returns>
        public static bool TryAddTags(this IEntity entity, params string[] tags)
        {
            return entity.TryGetComponent(out TagComponent tagComponent) &&
                   tagComponent.AddTags(tags);
        }

        /// <summary>
        ///     Tries to add the given tags to an entity if it has a
        ///     <see cref="TagComponent"/> and the tags don't already exist.
        /// </summary>
        /// <param name="entity">The entity to add the tag to.</param>
        /// <param name="tags">The tags to add.</param>
        /// <returns>
        ///     true if any tags were added, false otherwise even if they all already existed.
        /// </returns>
        public static bool TryAddTags(this IEntity entity, IEnumerable<string> tags)
        {
            return entity.TryGetComponent(out TagComponent tagComponent) &&
                   tagComponent.AddTags(tags);
        }

        /// <summary>
        ///     Checks if a tag has been added to an entity.
        /// </summary>
        /// <param name="entity">The entity to check.</param>
        /// <param name="tag">The tag to check for.</param>
        /// <returns>true if it exists, false otherwise.</returns>
        public static bool HasTag(this IEntity entity, string tag)
        {
            return entity.TryGetComponent(out TagComponent tagComponent) &&
                   tagComponent.HasTag(tag);
        }

        /// <summary>
        ///     Checks if all of the given tags have been added to an entity.
        /// </summary>
        /// <param name="entity">The entity to check.</param>
        /// <param name="tags">The tags to check for.</param>
        /// <returns>true if they all exist, false otherwise.</returns>
        public static bool HasAllTags(this IEntity entity, params string[] tags)
        {
            return entity.TryGetComponent(out TagComponent tagComponent) &&
                   tagComponent.HasAllTags(tags);
        }

        /// <summary>
        ///     Checks if all of the given tags have been added to an entity.
        /// </summary>
        /// <param name="entity">The entity to check.</param>
        /// <param name="tags">The tags to check for.</param>
        /// <returns>true if they all exist, false otherwise.</returns>
        public static bool HasAllTags(this IEntity entity, IEnumerable<string> tags)
        {
            return entity.TryGetComponent(out TagComponent tagComponent) &&
                   tagComponent.HasAllTags(tags);
        }

        /// <summary>
        ///     Checks if all of the given tags have been added to an entity.
        /// </summary>
        /// <param name="entity">The entity to check.</param>
        /// <param name="tags">The tags to check for.</param>
        /// <returns>true if any of them exist, false otherwise.</returns>
        public static bool HasAnyTags(this IEntity entity, params string[] tags)
        {
            return entity.TryGetComponent(out TagComponent tagComponent) &&
                   tagComponent.HasAnyTag(tags);
        }

        /// <summary>
        ///     Checks if all of the given tags have been added to an entity.
        /// </summary>
        /// <param name="entity">The entity to check.</param>
        /// <param name="tags">The tags to check for.</param>
        /// <returns>true if any of them exist, false otherwise.</returns>
        public static bool HasAnyTag(this IEntity entity, IEnumerable<string> tags)
        {
            return entity.TryGetComponent(out TagComponent tagComponent) &&
                   tagComponent.HasAnyTag(tags);
        }

        /// <summary>
        ///     Tries to remove a tag from an entity if it exists.
        /// </summary>
        /// <param name="entity">The entity to remove the tag from.</param>
        /// <param name="tag">The tag to remove.</param>
        /// <returns>
        ///     true if it was removed, false otherwise even if it didn't exist.
        /// </returns>
        public static bool RemoveTag(this IEntity entity, string tag)
        {
            return entity.TryGetComponent(out TagComponent tagComponent) &&
                   tagComponent.RemoveTag(tag);
        }

        /// <summary>
        ///     Tries to remove a tag from an entity if it exists.
        /// </summary>
        /// <param name="entity">The entity to remove the tag from.</param>
        /// <param name="tags">The tag to remove.</param>
        /// <returns>
        ///     true if it was removed, false otherwise even if it didn't exist.
        /// </returns>
        public static bool RemoveTags(this IEntity entity, params string[] tags)
        {
            return entity.TryGetComponent(out TagComponent tagComponent) &&
                   tagComponent.RemoveTags(tags);
        }

        /// <summary>
        ///     Tries to remove a tag from an entity if it exists.
        /// </summary>
        /// <param name="entity">The entity to remove the tag from.</param>
        /// <param name="tags">The tag to remove.</param>
        /// <returns>
        ///     true if it was removed, false otherwise even if it didn't exist.
        /// </returns>
        public static bool RemoveTags(this IEntity entity, IEnumerable<string> tags)
        {
            return entity.TryGetComponent(out TagComponent tagComponent) &&
                   tagComponent.RemoveTags(tags);
        }
    }
}
