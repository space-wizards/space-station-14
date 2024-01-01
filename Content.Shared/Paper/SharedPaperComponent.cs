using Robust.Shared.Serialization;

namespace Content.Shared.Paper;

public abstract partial class SharedPaperComponent : Component
{
    [Serializable, NetSerializable]
    public sealed class PaperBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly string Text;
        public readonly List<StampDisplayInfo> StampedBy;
        public readonly PaperAction Mode;

        public PaperBoundUserInterfaceState(string text, List<StampDisplayInfo> stampedBy, PaperAction mode = PaperAction.Read)
        {
            Text = text;
            StampedBy = stampedBy;
            Mode = mode;
        }
    }

    [Serializable, NetSerializable]
    public sealed class PaperInputTextMessage : BoundUserInterfaceMessage
    {
        public readonly string Text;

        public PaperInputTextMessage(string text)
        {
            Text = text;
        }
    }

    public sealed class ReadAttemptEvent : EventArgs
    {
        /// <summary>
        /// Should the reader be able understand what is written
        /// </summary>
        public bool CanRead;

        /// <summary>
        /// Who is attmpting to read
        /// </summary>
        public EntityUid? Reader;

        /// <summary>
        /// What are they attempting to read
        /// </summary>
        public EntityUid? EntityRead;

        public ReadAttemptEvent(EntityUid? reader, EntityUid? entityRead)
        {
            Reader = reader;
            EntityRead = entityRead;
            CanRead = true;
        }
    }

    public sealed class WriteAttemptEvent : EventArgs
    {
        /// <summary>
        /// Can the writer actually write
        /// </summary>
        public bool CanWrite;

        /// <summary>
        /// Who is attmpting to write
        /// </summary>
        public EntityUid? Writer;

        /// <summary>
        /// What are they attempting to write on
        /// </summary>
        public EntityUid? EntityWritten;

        public WriteAttemptEvent(EntityUid? writer, EntityUid? entityWritten)
        {
            Writer = writer;
            EntityWritten = entityWritten;
            CanWrite = true;
        }
    }

    [Serializable, NetSerializable]
    public enum PaperUiKey
    {
        Key
    }

    [Serializable, NetSerializable]
    public enum PaperAction
    {
        Read,
        Write,
    }

    [Serializable, NetSerializable]
    public enum PaperVisuals : byte
    {
        Status,
        Stamp
    }

    [Serializable, NetSerializable]
    public enum PaperStatus : byte
    {
        Blank,
        Written
    }
}
