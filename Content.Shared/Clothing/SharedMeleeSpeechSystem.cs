using Robust.Shared.Serialization;

namespace Content.Shared.Clothing
{
    public abstract class SharedMeleeSpeechSystem : EntitySystem
    {
        // Just for friending for now
    }

    /// <summary>
    /// Key representing which <see cref="BoundUserInterface"/> is currently open.
    /// Useful when there are multiple UI for an object. Here it's future-proofing only.
    /// </summary>
    [Serializable, NetSerializable]
    public enum NorthStarUiKey : byte
    {
        Key,
    }

    /// <summary>
    /// Represents an <see cref="MeleeSpeechComponent"/> state that can be sent to the client
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class MeleeSpeechBoundUserInterfaceState : BoundUserInterfaceState
    {
        public string CurrentBattlecry { get; }

        public MeleeSpeechBoundUserInterfaceState(string currentBattlecry)
        {
            CurrentBattlecry = currentBattlecry;
        }
    }

    [Serializable, NetSerializable]
    public sealed class MeleeSpeechBattlecryChangedMessage : BoundUserInterfaceMessage
    {
        public string Battlecry { get; }
        public MeleeSpeechBattlecryChangedMessage(string battlecry)
        {
            Battlecry = battlecry;
        }
    }
}
