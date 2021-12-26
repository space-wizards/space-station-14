using Content.Shared.Access.Components;
using Robust.Shared.GameObjects;
using System.Collections.Generic;

namespace Content.Shared.Access.Systems
{
    public class AccessSystem : EntitySystem
    {
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
    }
}
