using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Shared.Tag
{
    public static class TagComponentExtensions
    {
        /// <summary>
        ///     Tries to add a tag to an entity if the tag doesn't already exist.
        /// </summary>
        /// <param name="entity">The entity to add the tag to.</param>
        /// <param name="id">The tag to add.</param>
        /// <returns>
        ///     true if it was added, false otherwise even if it already existed.
        /// </returns>
        /// <exception cref="UnknownPrototypeException">
        ///     Thrown if no <see cref="TagPrototype"/> exists with the given id.
        /// </exception>
        public static bool AddTag(this EntityUid entity, string id)
        {
            return entity.EnsureComponent(out TagComponent tagComponent) &&
                   tagComponent.AddTag(id);
        }

        /// <summary>
        ///     Tries to add the given tags to an entity if the tags don't already exist.
        /// </summary>
        /// <param name="entity">The entity to add the tag to.</param>
        /// <param name="ids">The tags to add.</param>
        /// <returns>
        ///     true if any tags were added, false otherwise even if they all already existed.
        /// </returns>
        /// <exception cref="UnknownPrototypeException">
        ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
        /// </exception>
        public static bool AddTags(this EntityUid entity, params string[] ids)
        {
            return entity.EnsureComponent(out TagComponent tagComponent) &&
                   tagComponent.AddTags(ids);
        }

        /// <summary>
        ///     Tries to add the given tags to an entity if the tags don't already exist.
        /// </summary>
        /// <param name="entity">The entity to add the tag to.</param>
        /// <param name="ids">The tags to add.</param>
        /// <returns>
        ///     true if any tags were added, false otherwise even if they all already existed.
        /// </returns>
        /// <exception cref="UnknownPrototypeException">
        ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
        /// </exception>
        public static bool AddTags(this EntityUid entity, IEnumerable<string> ids)
        {
            return entity.EnsureComponent(out TagComponent tagComponent) &&
                   tagComponent.AddTags(ids);
        }

        /// <summary>
        ///     Tries to add a tag to an entity if it has a <see cref="TagComponent"/>
        ///     and the tag doesn't already exist.
        /// </summary>
        /// <param name="entity">The entity to add the tag to.</param>
        /// <param name="id">The tag to add.</param>
        /// <returns>
        ///     true if it was added, false otherwise even if it already existed.
        /// </returns>
        /// <exception cref="UnknownPrototypeException">
        ///     Thrown if no <see cref="TagPrototype"/> exists with the given id.
        /// </exception>
        public static bool TryAddTag(this EntityUid entity, string id)
        {
            return IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out TagComponent? tagComponent) &&
                   tagComponent.AddTag(id);
        }

        /// <summary>
        ///     Tries to add the given tags to an entity if it has a
        ///     <see cref="TagComponent"/> and the tags don't already exist.
        /// </summary>
        /// <param name="entity">The entity to add the tag to.</param>
        /// <param name="ids">The tags to add.</param>
        /// <returns>
        ///     true if any tags were added, false otherwise even if they all already existed.
        /// </returns>
        /// <exception cref="UnknownPrototypeException">
        ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
        /// </exception>
        public static bool TryAddTags(this EntityUid entity, params string[] ids)
        {
            return IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out TagComponent? tagComponent) &&
                   tagComponent.AddTags(ids);
        }

        /// <summary>
        ///     Tries to add the given tags to an entity if it has a
        ///     <see cref="TagComponent"/> and the tags don't already exist.
        /// </summary>
        /// <param name="entity">The entity to add the tag to.</param>
        /// <param name="ids">The tags to add.</param>
        /// <returns>
        ///     true if any tags were added, false otherwise even if they all already existed.
        /// </returns>
        /// <exception cref="UnknownPrototypeException">
        ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
        /// </exception>
        public static bool TryAddTags(this EntityUid entity, IEnumerable<string> ids)
        {
            return IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out TagComponent? tagComponent) &&
                   tagComponent.AddTags(ids);
        }

        /// <summary>
        ///     Checks if a tag has been added to an entity.
        /// </summary>
        /// <param name="entity">The entity to check.</param>
        /// <param name="id">The tag to check for.</param>
        /// <returns>true if it exists, false otherwise.</returns>
        /// <exception cref="UnknownPrototypeException">
        ///     Thrown if no <see cref="TagPrototype"/> exists with the given id.
        /// </exception>
        public static bool HasTag(this EntityUid entity, string id)
        {
            return IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out TagComponent? tagComponent) &&
                   tagComponent.HasTag(id);
        }

        /// <summary>
        ///     Checks if all of the given tags have been added to an entity.
        /// </summary>
        /// <param name="entity">The entity to check.</param>
        /// <param name="ids">The tags to check for.</param>
        /// <returns>true if they all exist, false otherwise.</returns>
        /// <exception cref="UnknownPrototypeException">
        ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
        /// </exception>
        public static bool HasAllTags(this EntityUid entity, params string[] ids)
        {
            return IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out TagComponent? tagComponent) &&
                   tagComponent.HasAllTags(ids);
        }

        /// <summary>
        ///     Checks if all of the given tags have been added to an entity.
        /// </summary>
        /// <param name="entity">The entity to check.</param>
        /// <param name="ids">The tags to check for.</param>
        /// <returns>true if they all exist, false otherwise.</returns>
        /// <exception cref="UnknownPrototypeException">
        ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
        /// </exception>
        public static bool HasAllTags(this EntityUid entity, IEnumerable<string> ids)
        {
            return IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out TagComponent? tagComponent) &&
                   tagComponent.HasAllTags(ids);
        }

        /// <summary>
        ///     Checks if all of the given tags have been added to an entity.
        /// </summary>
        /// <param name="entity">The entity to check.</param>
        /// <param name="ids">The tags to check for.</param>
        /// <returns>true if any of them exist, false otherwise.</returns>
        /// <exception cref="UnknownPrototypeException">
        ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
        /// </exception>
        public static bool HasAnyTag(this EntityUid entity, params string[] ids)
        {
            return IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out TagComponent? tagComponent) &&
                   tagComponent.HasAnyTag(ids);
        }

        /// <summary>
        ///     Checks if all of the given tags have been added to an entity.
        /// </summary>
        /// <param name="entity">The entity to check.</param>
        /// <param name="ids">The tags to check for.</param>
        /// <returns>true if any of them exist, false otherwise.</returns>
        /// <exception cref="UnknownPrototypeException">
        ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
        /// </exception>
        public static bool HasAnyTag(this EntityUid entity, IEnumerable<string> ids)
        {
            return IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out TagComponent? tagComponent) &&
                   tagComponent.HasAnyTag(ids);
        }

        /// <summary>
        ///     Tries to remove a tag from an entity if it exists.
        /// </summary>
        /// <param name="entity">The entity to remove the tag from.</param>
        /// <param name="id">The tag to remove.</param>
        /// <returns>
        ///     true if it was removed, false otherwise even if it didn't exist.
        /// </returns>
        /// <exception cref="UnknownPrototypeException">
        ///     Thrown if no <see cref="TagPrototype"/> exists with the given id.
        /// </exception>
        public static bool RemoveTag(this EntityUid entity, string id)
        {
            return IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out TagComponent? tagComponent) &&
                   tagComponent.RemoveTag(id);
        }

        /// <summary>
        ///     Tries to remove a tag from an entity if it exists.
        /// </summary>
        /// <param name="entity">The entity to remove the tag from.</param>
        /// <param name="ids">The tag to remove.</param>
        /// <returns>
        ///     true if it was removed, false otherwise even if it didn't exist.
        /// <exception cref="UnknownPrototypeException">
        ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
        /// </exception>
        /// </returns>
        public static bool RemoveTags(this EntityUid entity, params string[] ids)
        {
            return IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out TagComponent? tagComponent) &&
                   tagComponent.RemoveTags(ids);
        }

        /// <summary>
        ///     Tries to remove a tag from an entity if it exists.
        /// </summary>
        /// <param name="entity">The entity to remove the tag from.</param>
        /// <param name="ids">The tag to remove.</param>
        /// <returns>
        ///     true if it was removed, false otherwise even if it didn't exist.
        /// </returns>
        /// <exception cref="UnknownPrototypeException">
        ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
        /// </exception>
        public static bool RemoveTags(this EntityUid entity, IEnumerable<string> ids)
        {
            return IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out TagComponent? tagComponent) &&
                   tagComponent.RemoveTags(ids);
        }
    }
}
