using System;
using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Chemistry
{
    [Serializable, NetSerializable]
    public class TransferAmountEuiMessage : EuiMessageBase
    {
        public int Value;
        public TransferAmountEuiMessage(int value)
        {
            Value = value;
        }
    }
}
