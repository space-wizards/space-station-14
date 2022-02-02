using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Paper
{
    public class SharedPaperComponent : Component
    {
        [Serializable, NetSerializable]
        public class PaperBoundUserInterfaceState : BoundUserInterfaceState
        {
            public readonly string Text;
            public readonly PaperAction Mode;

            public PaperBoundUserInterfaceState(string text, PaperAction mode = PaperAction.Read)
            {
                Text = text;
                Mode = mode;
            }
        }

        [Serializable, NetSerializable]
        public class PaperActionMessage : BoundUserInterfaceMessage
        {
            public readonly PaperAction Action;
            public PaperActionMessage(PaperAction action)
            {
                Action = action;
            }
        }

        [Serializable, NetSerializable]
        public class PaperInputText : BoundUserInterfaceMessage
        {
            public readonly string Text;

            public PaperInputText(string text)
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
            CrossOut,
            Stamp
        }

        [Serializable, NetSerializable]
        public enum PaperVisuals : byte
        {
            Status
        }

        [Serializable, NetSerializable]
        public enum PaperStatus : byte
        {
            Blank,
            Written
        }

    }
}
