using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Surgery.UI
{
    [Serializable, NetSerializable]
    public class SurgeryOpSlotSelectUIMsg
    {
        public SurgeryOpSlotSelectUIMsg(EntityUid body, string slot)
        {
            Body = body;
            Slot = slot;
        }

        public EntityUid Body { get; }

        public string Slot { get; }
    }
}
