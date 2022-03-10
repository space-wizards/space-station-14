using Robust.Shared.Serialization;

namespace Content.Shared.Speech.Systems
{
    public class SharedVoiceChangerSystem : EntitySystem
    {
        /// Just for friending for now
    }
    /// <summary>
    /// Key representing which <see cref="BoundUserInterface"/> is currently open.
    /// Useful when there are multiple UI for an object. Here it's future-proofing only.
    /// </summary>
    [Serializable, NetSerializable]
    public enum VoiceChangerUiKey
    {
        Key,
    }

    /// <summary>
    /// Represents an <see cref="VoiceChangerComponent"/> state that can be sent to the client
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class VoiceChangerBoundUserInterfaceState : BoundUserInterfaceState
    {
        public string CurrentName { get; }

        public VoiceChangerBoundUserInterfaceState(string currentName)
        {
            CurrentName = currentName;
        }
    }

    [Serializable, NetSerializable]
    public sealed class VoiceChangerNameChangedMessage : BoundUserInterfaceMessage
    {
        public string Name { get; }

        public VoiceChangerNameChangedMessage(string name)
        {
            Name = name;
        }
    }
}
