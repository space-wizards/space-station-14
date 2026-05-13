using Robust.Shared.Serialization;

namespace Content.Shared.Nuke
{
    public abstract partial class SharedNukeComponent : Component
    {
        public const string NukeDiskSlotId = "Nuke";

        /// <summary>
        /// Cooldown time between attempts to enter the nuke code.
        /// Used to prevent clients from trying to brute force it.
        /// </summary>
        public static readonly TimeSpan EnterCodeCooldown = TimeSpan.FromSeconds(1);
    }

    [Serializable, NetSerializable]
    public sealed partial class NukeAnchorMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public sealed partial class NukeKeypadMessage : BoundUserInterfaceMessage
    {
        public int Value;

        public NukeKeypadMessage(int value)
        {
            Value = value;
        }
    }

    [Serializable, NetSerializable]
    public sealed partial class NukeKeypadClearMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public sealed partial class NukeKeypadEnterMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public sealed partial class NukeArmedMessage : BoundUserInterfaceMessage
    {
    }
}

