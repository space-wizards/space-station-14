using System;
using Content.Shared.Chemistry;
using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Chemistry
{
    [Serializable, NetSerializable]
    public class TransferAmountEuiMessage : EuiMessageBase
    {
        public ReagentUnit Value;
        public TransferAmountEuiMessage(ReagentUnit value)
        {
            Value = value;
        }
    }
}
