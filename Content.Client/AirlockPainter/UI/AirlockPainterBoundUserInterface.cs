using Content.Shared.AirlockPainter;
using Robust.Client.GameObjects;

namespace Content.Client.AirlockPainter.UI
{
    public sealed class AirlockPainterBoundUserInterface : BoundUserInterface
    {
        private AirlockPainterWindow? _window;
        public List<string> Styles = new();

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

        private void OnSpritePicked(int? index)
        {
            if (index == null) return;
            SendMessage(new AirlockPainterSpritePickedMessage(index.Value));
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (_window == null || state is not AirlockPainterBoundUserInterfaceState cast)
                return;

            _window.Populate(cast.Styles);
            _window.SelectedStyle(cast.Styles[cast.SelectedIndex]);
        }
    }
}
