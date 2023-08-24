using Robust.Shared.Serialization;

namespace Content.Shared.Nuke
{
    public abstract partial class SharedNukeComponent : Component
    {
        public const string NukeDiskSlotId = "Nuke";
    }

    [Serializable, NetSerializable]
    public sealed class NukeAnchorMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public sealed class NukeKeypadMessage : BoundUserInterfaceMessage
    {
        public int Value;

        public NukeKeypadMessage(int value)
        {
            Value = value;
        }
    }

    [Serializable, NetSerializable]
    public sealed class NukeKeypadClearMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public sealed class NukeKeypadEnterMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public sealed class NukeArmedMessage : BoundUserInterfaceMessage
    {
    }
}
