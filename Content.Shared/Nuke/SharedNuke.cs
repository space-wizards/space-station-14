using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Nuke
{
    [Serializable, NetSerializable]
    public enum NukeUiKey : byte
    {
        Key
    }

    public enum NukeStatus : byte
    {
        AWAIT_DISK,
        AWAIT_CODE,
        AWAIT_ARM,
        ARMED
    }

    [Serializable, NetSerializable]
    public class NukeUiState : BoundUserInterfaceState
    {
        public bool DiskInserted;
        public NukeStatus Status;
        public int RemainingTime;
        public bool IsAnchored;
        public int EnteredCodeLength;
        public int MaxCodeLength;
        public bool AllowArm;
    }
}
