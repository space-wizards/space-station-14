using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Nuke
{
    public enum NukeVisualLayers
    {
        Base,
        Unlit
    }

    [NetSerializable, Serializable]
    public enum NukeVisuals
    {
        Deployed,
        State,
    }

    [NetSerializable, Serializable]
    public enum NukeVisualState
    {
        Idle,
        Armed,
        YoureFucked
    }

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
        ARMED,
        COOLDOWN
    }

    [Serializable, NetSerializable]
    public sealed class NukeUiState : BoundUserInterfaceState
    {
        public bool DiskInserted;
        public NukeStatus Status;
        public int RemainingTime;
        public int CooldownTime;
        public bool IsAnchored;
        public int EnteredCodeLength;
        public int MaxCodeLength;
        public bool AllowArm;
    }

    [Serializable, NetSerializable]
    public sealed partial class NukeDisarmDoAfterEvent : SimpleDoAfterEvent
    {
    }
}
