using Content.Shared.AirlockPainter;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.AirlockPainter.UI
{
    public sealed class AirlockPainterBoundUserInterface : BoundUserInterface
    {
        [Dependency] IPrototypeManager _prototypeManager = default!;

        private AirlockPainterWindow? _window;

        public AirlockPainterBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = new AirlockPainterWindow(_prototypeManager);
            if (State != null)
                UpdateState(State);

            _window.OpenCentered();

            _window.OnClose += Close;
            _window.OnSpritePicked += OnSpritePicked;
        }

        private void OnSpritePicked(string? val)
        {
            if (val != null)
                SendMessage(new AirlockPainterSpritePickedMessage(val));
            Close();
        }
    }
}
