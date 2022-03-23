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
}
