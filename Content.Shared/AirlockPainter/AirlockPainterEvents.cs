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
        public int SelectedIndex;
        public List<string> Styles;

        public AirlockPainterBoundUserInterfaceState(int selectedIndex, List<string> styles)
        {
            SelectedIndex = selectedIndex;
            Styles = styles;
        }
    }
}
