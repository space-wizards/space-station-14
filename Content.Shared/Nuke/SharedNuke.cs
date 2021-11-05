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

    [Serializable, NetSerializable]
    public class NukeUiState : BoundUserInterfaceState
    {
        public bool NukeDiskInserted;
        public bool IsArmed;
        public int RemainingTime;
    }
}
