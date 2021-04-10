#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Prototypes.Tag;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Tag
{
    [RegisterComponent]
    public class TagComponent : Component, ISerializationHooks
    {
        public override string Name => "Tag";

        [ViewVariables]
        [DataField("tags", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<TagPrototype>))]
        private readonly HashSet<string> _tags = new();

        public IReadOnlySet<string> Tags => _tags;

        public override void Initialize()
        {
            base.Initialize();

            foreach (var tag in _tags)
            {
                GetTagOrThrow(tag);
            }
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            var tags = new string[_tags.Count];
            var i = 0;

            foreach (var tag in _tags)
            {
                tags[i] = tag;
            }

            return new TagComponentState(tags);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState is not TagComponentState state)
            {
                return;
            }

            _tags.Clear();

            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            foreach (var tag in state.Tags)
            {
                GetTagOrThrow(tag, prototypeManager);
                _tags.Add(tag);
            }
        }

        private TagPrototype GetTagOrThrow(string id, IPrototypeManager? manager = null)
        {
            manager ??= IoCManager.Resolve<IPrototypeManager>();
            return manager.Index<TagPrototype>(id);
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
            GetTagOrThrow(id);
            var added = _tags.Add(id);

            if (added)
            {
                Dirty();
                return true;
            }

            return false;
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
                GetTagOrThrow(id, prototypeManager);
                _tags.Add(id);
            }

            if (_tags.Count > count)
            {
                Dirty();
                return true;
            }

            return false;
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
            GetTagOrThrow(id);
            return _tags.Contains(id);
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
                GetTagOrThrow(id, prototypeManager);

                if (!_tags.Contains(id))
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
                GetTagOrThrow(id, prototypeManager);

                if (_tags.Contains(id))
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
            GetTagOrThrow(id);

            if (_tags.Remove(id))
            {
                Dirty();
                return true;
            }

            return false;
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
                GetTagOrThrow(id, prototypeManager);
                _tags.Remove(id);
            }

            if (_tags.Count < count)
            {
                Dirty();
                return true;
            }

            return false;
        }
    }
}
