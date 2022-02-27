using Content.Shared.Access.Components;
using Robust.Shared.GameObjects;
using System.Collections.Generic;
using Robust.Shared.Prototypes;

namespace Content.Shared.Access.Systems
{
    public sealed class AccessSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<AccessComponent, ComponentInit>(OnAccessInit);
        }

        private void OnAccessInit(EntityUid uid, AccessComponent component, ComponentInit args)
        {
            // Add all tags in groups to the list of tags.
            foreach (var group in component.Groups)
            {
                if (!_prototypeManager.TryIndex<AccessGroupPrototype>(group, out var proto))
                    continue;

                component.Tags.UnionWith(proto.Tags);
            }
        }

        /// <summary>
        ///     Replaces the set of access tags we have with the provided set.
        /// </summary>
        /// <param name="newTags">The new access tags</param>
        public bool TrySetTags(EntityUid uid, IEnumerable<string> newTags, AccessComponent? access = null)
        {
            if (!Resolve(uid, ref access))
                return false;

            access.Tags.Clear();
            access.Tags.UnionWith(newTags);

            return true;
        }

        public bool TryAddGroups(EntityUid uid, IEnumerable<string> newGroups, AccessComponent? access = null)
        {
            if (!Resolve(uid, ref access))
                return false;

            foreach (var group in newGroups)
            {
                if (!_prototypeManager.TryIndex<AccessGroupPrototype>(group, out var proto))
                    continue;

                access.Tags.UnionWith(proto.Tags);
            }

            return true;
        }
    }
}
