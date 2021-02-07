#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Suspicion
{
    public abstract class SharedSuspicionRoleComponent : Component
    {
        public sealed override string Name => "SuspicionRole";
        public sealed override uint? NetID => ContentNetIDs.SUSPICION_ROLE;
    }

    [Serializable, NetSerializable]
    public class SuspicionRoleComponentState : ComponentState
    {
        public readonly string? Role;
        public readonly bool? Antagonist;
        public readonly (string name, EntityUid)[] Allies;

        public SuspicionRoleComponentState(string? role, bool? antagonist, (string name, EntityUid)[] allies) : base(ContentNetIDs.SUSPICION_ROLE)
        {
            Role = role;
            Antagonist = antagonist;
            Allies = allies;
        }
    }
}
