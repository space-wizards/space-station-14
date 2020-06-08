using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Observer
{
    public class SharedGhostComponent : Component
    {
        public override string Name => "Ghost";
        public override uint? NetID => ContentNetIDs.GHOST;
    }

    [Serializable, NetSerializable]
    public class GhostComponentState : ComponentState
    {
        public bool CanReturnToBody { get; }
        public override uint NetID => ContentNetIDs.GHOST;

        public GhostComponentState(bool canReturnToBody)
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
