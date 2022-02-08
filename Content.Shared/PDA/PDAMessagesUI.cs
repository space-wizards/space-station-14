using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;

namespace Content.Shared.PDA
{
    [Serializable, NetSerializable]
    public sealed class PDAToggleFlashlightMessage : BoundUserInterfaceMessage
    {
        public PDAToggleFlashlightMessage()
        {

        }
    }

    [Serializable, NetSerializable]
    public sealed class PDAEjectIDMessage : BoundUserInterfaceMessage
    {
        public PDAEjectIDMessage()
        {

        }
    }

    [Serializable, NetSerializable]
    public sealed class PDAEjectPenMessage : BoundUserInterfaceMessage
    {
        public PDAEjectPenMessage()
        {

        }
    }
    [Serializable, NetSerializable]
    public sealed class PDAShowRingtoneMessage : BoundUserInterfaceMessage
    {
        public PDAShowRingtoneMessage()
        {

        }
    }

    [Serializable, NetSerializable]
    public sealed class PDAShowUplinkMessage : BoundUserInterfaceMessage
    {
        public PDAShowUplinkMessage()
        {

        }
    }


    [Serializable, NetSerializable]
    public sealed class PDARequestUpdateInterfaceMessage : BoundUserInterfaceMessage
    {
        public PDARequestUpdateInterfaceMessage()
        {

        }
    }
}
