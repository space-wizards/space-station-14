#nullable enable
using System;
using System.Collections.Generic;
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

        public SuspicionRoleComponentState(string? role, bool? antagonist) : base(ContentNetIDs.SUSPICION_ROLE)
        {
            Role = role;
            Antagonist = antagonist;
        }
    }

    [Serializable, NetSerializable]
    public class SuspicionAlliesMessage : ComponentMessage
    {
        public readonly HashSet<string> Allies;

        public SuspicionAlliesMessage(HashSet<string> allies)
        {
            Directed = true;
            Allies = allies;
        }
    }
}
