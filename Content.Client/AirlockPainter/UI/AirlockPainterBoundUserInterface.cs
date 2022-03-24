using Content.Shared.AirlockPainter;
using Robust.Client.GameObjects;

namespace Content.Client.AirlockPainter.UI
{
  public sealed class AirlockPainterBoundUserInterface : BoundUserInterface
  {
    private AirlockPainterWindow? _window;

    public AirlockPainterBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
      base.Open();

      _window = new AirlockPainterWindow();
      if (State != null)
        UpdateState(State);

      _window.OpenCentered();

      _window.OnClose += Close;
      _window.OnSpritePicked += OnSpritePicked;
    }

    private void OnSpritePicked(int index)
    {
      SendMessage(new AirlockPainterSpritePickedMessage(index));
      Close();
    }
  }
}
