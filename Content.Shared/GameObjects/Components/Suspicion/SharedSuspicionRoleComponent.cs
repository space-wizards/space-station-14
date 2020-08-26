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

        public SuspicionRoleComponentState(string? role, bool? antagonist) : base(ContentNetIDs.SUSPICION_ROLE)
        {
            Role = role;
            Antagonist = antagonist;
        }
    }

    [Serializable, NetSerializable]
    public class SuspicionAlliesMessage : ComponentMessage
    {
        public readonly HashSet<EntityUid> Allies;

        public SuspicionAlliesMessage(HashSet<EntityUid> allies)
        {
            Directed = true;
            Allies = allies;
        }

        public SuspicionAlliesMessage(IEnumerable<EntityUid> allies) : this(allies.ToHashSet()) { }
    }

    [Serializable, NetSerializable]
    public class SuspicionAllyAddedMessage : ComponentMessage
    {
        public readonly EntityUid Ally;

        public SuspicionAllyAddedMessage(EntityUid ally)
        {
            Directed = true;
            Ally = ally;
        }
    }

    [Serializable, NetSerializable]
    public class SuspicionAllyRemovedMessage : ComponentMessage
    {
        public readonly EntityUid Ally;

        public SuspicionAllyRemovedMessage(EntityUid ally)
        {
            Directed = true;
            Ally = ally;
        }
    }

    [Serializable, NetSerializable]
    public class SuspicionAlliesClearedMessage : ComponentMessage
    {
        public SuspicionAlliesClearedMessage()
        {
            Directed = true;
        }
    }
}
