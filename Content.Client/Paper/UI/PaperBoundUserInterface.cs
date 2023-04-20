using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Utility;
using static Content.Shared.Paper.SharedPaperComponent;

namespace Content.Client.Paper.UI
{
    [UsedImplicitly]
    public sealed class PaperBoundUserInterface : BoundUserInterface
    {
        private PaperWindow? _window;

        public PaperBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();
            var entityMgr = IoCManager.Resolve<IEntityManager>();

            _window = new PaperWindow();
            _window.OnClose += Close;
            _window.Input.OnKeyBindDown += args => // Solution while TextEdit don't have events
            {
                if (args.Function == EngineKeyFunctions.TextSubmit)
                {
                    var text = Rope.Collapse(_window.Input.TextRope);
                    Input_OnTextEntered(text);
                    args.Handle();
                }
            };

            if (entityMgr.TryGetComponent<PaperVisualsComponent>(Owner.Owner, out var visuals))
            {
                _window.InitVisuals(visuals);
            }

            _window.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            _window?.Populate((PaperBoundUserInterfaceState) state);
        }

        private void Input_OnTextEntered(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                SendMessage(new PaperInputTextMessage(text));

                if (_window != null)
                {
                    _window.Input.TextRope = Rope.Leaf.Empty;
                    _window.Input.CursorPosition = new TextEdit.CursorPos(0, TextEdit.LineBreakBias.Top);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _window?.Dispose();
        }
    }
}
