using Content.Server.Access.Components;
using Robust.Shared.GameObjects;
using System.Collections.Generic;

namespace Content.Server.Access.Systems
{
    public class AccessSystem : EntitySystem
    {
        public bool TrySetTags(EntityUid uid, IEnumerable<string> tags, AccessComponent? access = null)
        {
            if (!Resolve(uid, ref access))
                return false;

            access.Tags.Clear();
            access.Tags.UnionWith(tags);

            return true;
        }
    }
}
