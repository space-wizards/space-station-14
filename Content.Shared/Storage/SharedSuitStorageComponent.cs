using System;
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
            public readonly bool SuitPresent;
            public readonly bool Open;

            public SuitStorageBoundUserInterfaceState(bool open, bool suitPresent)
            {
                SuitPresent = suitPresent;
                Open = open;
            }
        }

        public enum UiButton
        {
            Open,
            Close
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
            public readonly int? ScanId;

            public SuitStorageUiButtonPressedMessage(UiButton button)
            {
                Button = button;
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
