using Content.Shared.Access.Components;
using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Access.Systems
{
    public abstract class SharedAccessSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<AccessComponent, MapInitEvent>(OnAccessInit);
            SubscribeLocalEvent<AccessComponent, GetAccessTagsEvent>(OnGetAccessTags);
        }

        private void OnAccessInit(EntityUid uid, AccessComponent component, MapInitEvent args)
        {
            // Add all tags in groups to the list of tags.
            foreach (var group in component.Groups)
            {
                if (!_prototypeManager.TryIndex<AccessGroupPrototype>(group, out var proto))
                    continue;

                component.Tags.UnionWith(proto.Tags);
                Dirty(uid, component);
            }
        }

        private void OnGetAccessTags(EntityUid uid, AccessComponent component, ref GetAccessTagsEvent args)
        {
            if (!component.Enabled)
                return;

            args.Tags.UnionWith(component.Tags);
        }

        public void SetAccessEnabled(EntityUid uid, bool val, AccessComponent? component = null)
        {
            if (!Resolve(uid, ref component, false))
                return;
            component.Enabled = val;
            Dirty(uid, component);
        }

        /// <summary>
        ///     Replaces the set of access tags we have with the provided set.
        /// </summary>
        /// <param name="access">The new access tags</param>
        public bool TrySetTags(EntityUid uid, IEnumerable<ProtoId<AccessLevelPrototype>> newTags, AccessComponent? access = null)
        {
            if (!Resolve(uid, ref access))
                return false;

            access.Tags.Clear();
            access.Tags.UnionWith(newTags);
            Dirty(uid, access);

            return true;
        }

        /// <summary>
        ///     Gets the set of access tags.
        /// </summary>
        /// <param name="access">The new access tags</param>
        public IEnumerable<ProtoId<AccessLevelPrototype>>? TryGetTags(EntityUid uid, AccessComponent? access = null)
        {
            return !Resolve(uid, ref access) ? null : access.Tags;
        }

        public bool TryAddGroups(EntityUid uid, IEnumerable<ProtoId<AccessGroupPrototype>> newGroups, AccessComponent? access = null)
        {
            if (!Resolve(uid, ref access))
                return false;

            foreach (var group in newGroups)
            {
                if (!_prototypeManager.TryIndex<AccessGroupPrototype>(group, out var proto))
                    continue;

                access.Tags.UnionWith(proto.Tags);
            }

            Dirty(uid, access);
            return true;
        }

        /// <summary>
        /// Set the access on an <see cref="AccessComponent"/> to the access for a specific job.
        /// </summary>
        /// <param name="uid">The ID of the entity with the access component.</param>
        /// <param name="prototype">The job prototype to use access from.</param>
        /// <param name="extended">Whether to apply extended job access.</param>
        /// <param name="access">The access component.</param>
        public void SetAccessToJob(
            EntityUid uid,
            JobPrototype prototype,
            bool extended,
            AccessComponent? access = null)
        {
            if (!Resolve(uid, ref access))
                return;

            access.Tags.Clear();
            access.Tags.UnionWith(prototype.Access);
            Dirty(uid, access);

            TryAddGroups(uid, prototype.AccessGroups, access);

            if (extended)
            {
                access.Tags.UnionWith(prototype.ExtendedAccess);
                TryAddGroups(uid, prototype.ExtendedAccessGroups, access);
            }
        }
    }
}
