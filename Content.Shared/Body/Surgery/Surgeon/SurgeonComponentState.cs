using System;
using Content.Shared.NetIDs;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Surgery.Surgeon
{
    [Serializable, NetSerializable]
    public class SurgeonComponentState : ComponentState
    {
        public SurgeonComponentState(EntityUid? target, EntityUid? mechanism) : base(ContentNetIDs.SURGEON)
        {
            Target = target;
            Mechanism = mechanism;
        }

        public EntityUid? Target { get; }

        public EntityUid? Mechanism { get; }
    }
}
