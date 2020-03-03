using System;
using Content.Shared.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Observer
{
    public class SharedGhostComponent : Component
    {
        public override string Name => "Ghost";
        public override uint? NetID => ContentNetIDs.GHOST;

        public virtual bool CanReturnToBody { get; set; } = true;
    }

    [Serializable, NetSerializable]
    public class GhostComponentState : ComponentState
    {
        public bool CanReturnToBody { get; }

        public GhostComponentState(bool canReturnToBody) : base(ContentNetIDs.GHOST)
        {
            CanReturnToBody = canReturnToBody;
        }
    }

    [Serializable, NetSerializable]
    public class ReturnToBodyComponentMessage : ComponentMessage
    {
        public ReturnToBodyComponentMessage() => Directed = true;
    }
}
