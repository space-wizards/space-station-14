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
    public List<string> SpriteList { get; set; } = new();

    public AirlockPainterBoundUserInterfaceState(List<string> spriteList)
    {
      this.SpriteList = spriteList;
    }
  }
}
