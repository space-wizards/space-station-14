using Robust.Shared.Serialization;

namespace Content.Shared.Paper
{
    public abstract class SharedPaperComponent : Component
    {
        [Serializable, NetSerializable]
        public sealed class PaperBoundUserInterfaceState : BoundUserInterfaceState
        {
            public readonly string Text;
            public readonly List<string> StampedBy;
            public readonly PaperAction Mode;

            public PaperBoundUserInterfaceState(string text, List<string> stampedBy, PaperAction mode = PaperAction.Read)
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
}
