#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Prototypes.Tag;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Tag
{
    [RegisterComponent]
    public class TagComponent : Component
    {
        public override string Name => "Tag";

        [ViewVariables]
        private readonly HashSet<TagPrototype> _tags = new();

        public IReadOnlySet<TagPrototype> Tags => _tags;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction(
                "tags",
                null!,
                (ids) =>
                {
                    _tags.Clear();

                    if (ids == null)
                    {
                        return;
                    }

                    AddTags(ids);
                },
                () =>
                {
                    var ids = new HashSet<string>();

                    foreach (var tag in _tags)
                    {
                        ids.Add(tag.ID);
                    }

                    return ids;
                });
        }

        /// <summary>
        ///     Tries to add a tag if it doesn't already exist.
        /// </summary>
        /// <param name="id">The tag to add.</param>
        /// <returns>true if it was added, false if it already existed.</returns>
        /// <exception cref="UnknownPrototypeException">
        ///     Thrown if no <see cref="TagPrototype"/> exists with the given id.
        /// </exception>
        public bool AddTag(string id)
        {
            var tag = IoCManager.Resolve<IPrototypeManager>().Index<TagPrototype>(id);

            return _tags.Add(tag);
        }

        /// <summary>
        ///     Tries to add the given tags if they don't already exist.
        /// </summary>
        /// <param name="ids">The tags to add.</param>
        /// <returns>true if any tags were added, false if they all already existed.</returns>
        /// <exception cref="UnknownPrototypeException">
        ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
        /// </exception>
        public bool AddTags(params string[] ids)
        {
            return AddTags(ids.AsEnumerable());
        }

        /// <summary>
        ///     Tries to add the given tags if they don't already exist.
        /// </summary>
        /// <param name="ids">The tags to add.</param>
        /// <returns>true if any tags were added, false if they all already existed.</returns>
        /// <exception cref="UnknownPrototypeException">
        ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
        /// </exception>
        public bool AddTags(IEnumerable<string> ids)
        {
            var count = _tags.Count;
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            foreach (var id in ids)
            {
                var tag = prototypeManager.Index<TagPrototype>(id);

                _tags.Add(tag);
            }

            return _tags.Count > count;
        }

        /// <summary>
        ///     Checks if a tag has been added.
        /// </summary>
        /// <param name="id">The tag to check for.</param>
        /// <returns>true if it exists, false otherwise.</returns>
        /// <exception cref="UnknownPrototypeException">
        ///     Thrown if no <see cref="TagPrototype"/> exists with the given id.
        /// </exception>
        public bool HasTag(string id)
        {
            var tag = IoCManager.Resolve<IPrototypeManager>().Index<TagPrototype>(id);

            return _tags.Contains(tag);
        }

        /// <summary>
        ///     Checks if all of the given tags have been added.
        /// </summary>
        /// <param name="ids">The tags to check for.</param>
        /// <returns>true if they all exist, false otherwise.</returns>
        /// <exception cref="UnknownPrototypeException">
        ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
        /// </exception>
        public bool HasAllTags(params string[] ids)
        {
            return HasAllTags(ids.AsEnumerable());
        }

        /// <summary>
        ///     Checks if all of the given tags have been added.
        /// </summary>
        /// <param name="ids">The tags to check for.</param>
        /// <returns>true if they all exist, false otherwise.</returns>
        /// <exception cref="UnknownPrototypeException">
        ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
        /// </exception>
        public bool HasAllTags(IEnumerable<string> ids)
        {
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            foreach (var id in ids)
            {
                var tag = prototypeManager.Index<TagPrototype>(id);

                if (!_tags.Contains(tag))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Checks if any of the given tags have been added.
        /// </summary>
        /// <param name="ids">The tags to check for.</param>
        /// <returns>true if any of them exist, false otherwise.</returns>
        /// <exception cref="UnknownPrototypeException">
        ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
        /// </exception>
        public bool HasAnyTag(params string[] ids)
        {
            return HasAnyTag(ids.AsEnumerable());
        }

        /// <summary>
        ///     Checks if any of the given tags have been added.
        /// </summary>
        /// <param name="ids">The tags to check for.</param>
        /// <returns>true if any of them exist, false otherwise.</returns>
        /// <exception cref="UnknownPrototypeException">
        ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
        /// </exception>
        public bool HasAnyTag(IEnumerable<string> ids)
        {
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            foreach (var id in ids)
            {
                var tag = prototypeManager.Index<TagPrototype>(id);

                if (_tags.Contains(tag))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Tries to remove a tag if it exists.
        /// </summary>
        /// <param name="id">The tag to remove.</param>
        /// <returns>
        ///     true if it was removed, false otherwise even if it didn't exist.
        /// </returns>
        /// <exception cref="UnknownPrototypeException">
        ///     Thrown if no <see cref="TagPrototype"/> exists with the given id.
        /// </exception>
        public bool RemoveTag(string id)
        {
            var tag = IoCManager.Resolve<IPrototypeManager>().Index<TagPrototype>(id);

            return _tags.Remove(tag);
        }

        /// <summary>
        ///     Tries to remove all of the given tags if they exist.
        /// </summary>
        /// <param name="ids">The tags to remove.</param>
        /// <returns>
        ///     true if it was removed, false otherwise even if they didn't exist.
        /// </returns>
        /// <exception cref="UnknownPrototypeException">
        ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
        /// </exception>
        public bool RemoveTags(params string[] ids)
        {
            return RemoveTags(ids.AsEnumerable());
        }

        /// <summary>
        ///     Tries to remove all of the given tags if they exist.
        /// </summary>
        /// <param name="ids">The tags to remove.</param>
        /// <returns>true if any tag was removed, false otherwise.</returns>
        /// <exception cref="UnknownPrototypeException">
        ///     Thrown if one of the ids represents an unregistered <see cref="TagPrototype"/>.
        /// </exception>
        public bool RemoveTags(IEnumerable<string> ids)
        {
            var count = _tags.Count;
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            foreach (var id in ids)
            {
                var tag = prototypeManager.Index<TagPrototype>(id);

                _tags.Remove(tag);
            }

            return _tags.Count < count;
        }
    }
}
