using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.AirlockPainter
{
    [Serializable, NetSerializable]
    public enum AirlockPainterUiKey
    {
        Key,
    }

    [Serializable, NetSerializable]
    public sealed class AirlockPainterSpritePickedMessage : BoundUserInterfaceMessage
    {
        public int Index { get; }

        public AirlockPainterSpritePickedMessage(int index)
        {
            Index = index;
        }
    }

    [Serializable, NetSerializable]
    public sealed class AirlockPainterBoundUserInterfaceState : BoundUserInterfaceState
    {
        public int SelectedStyle { get; }

        public AirlockPainterBoundUserInterfaceState(int selectedStyle)
        {
            SelectedStyle = selectedStyle;
        }
    }

    [Serializable, NetSerializable]
    public sealed class AirlockPainterDoAfterEvent : DoAfterEvent
    {
        [DataField("sprite", required: true)]
        public readonly string Sprite = default!;

        private AirlockPainterDoAfterEvent()
        {
        }

        public AirlockPainterDoAfterEvent(string sprite)
        {
            Sprite = sprite;
        }

        public override DoAfterEvent Clone() => this;
    }
}
