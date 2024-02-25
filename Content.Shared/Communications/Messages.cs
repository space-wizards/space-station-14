using Robust.Shared.Serialization;

namespace Content.Shared.Communications
{
    /// <summary>
    /// Raised when an announcement is attempted by a communications console.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class AttemptCommunicationConsoleAnnouncementMessage : EntityEventArgs
    {
        public NetEntity Console;
        public NetEntity Sender;
        public readonly string Message;

        public AttemptCommunicationConsoleAnnouncementMessage(NetEntity console, NetEntity sender, string message)
        {
            Console = console;
            Sender = sender;
            Message = message;
        }
    }
}

