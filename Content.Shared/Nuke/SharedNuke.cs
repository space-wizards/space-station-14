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
        AWAIT_TIMER,
        AWAIT_ARM,
        TIMING
    }

    [Serializable, NetSerializable]
    public class NukeUiState : BoundUserInterfaceState
    {
        public NukeStatus Status;
        public string Code = "";
        public int RemainingTime;
    }
}
