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
        public readonly TagsState? TagsState;

        public PaperBoundUserInterfaceState(string text, TagsState? tagsState, List<StampDisplayInfo> stampedBy, PaperAction mode = PaperAction.Read)
        {
            Text = text;
            StampedBy = stampedBy;
            Mode = mode;
            TagsState = tagsState;
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

    [Serializable, NetSerializable]
    public record TagsState(TimeSpan? WriteTime, string? StationName, string? PersonName, string? PersonJob, DateTime? WriteDate);

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
