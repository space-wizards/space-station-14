using System;
using System.Linq;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Storage
{
    public class SharedSuitStorageComponent : Component
    {
        public override string Name => "SuitStorage";

        [Serializable, NetSerializable]
        public class SuitStorageBoundUserInterfaceState : BoundUserInterfaceState
        {
            public readonly Dictionary<int, string?> Contents;
            public readonly bool UiKnownPowerState;
            public readonly bool Open;

            public SuitStorageBoundUserInterfaceState(Dictionary<int, string?> contents, bool open, bool uiKnownPowerState)
            {
                Open = open;
                Contents = contents;
                UiKnownPowerState = uiKnownPowerState;
            }
        }

        public enum UiButton
        {
            Open,
            Close,
            Dispense
        }

        [Serializable, NetSerializable]
        public enum SuitStorageUIKey
        {
            Key
        }

        [Serializable, NetSerializable]
        public class SuitStorageUiButtonPressedMessage : BoundUserInterfaceMessage
        {
            public readonly UiButton Button;
            public readonly int? ItemId;

            public SuitStorageUiButtonPressedMessage(UiButton button, int? itemId = null)
            {
                Button = button;
                ItemId = itemId;
            }
        }
    }

    [NetSerializable]
    [Serializable]
    public enum SuitStorageVisuals
    {
        Open
    }
}
