using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
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
            _window.Input.OnTextEntered += Input_OnTextEntered;

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

        private void Input_OnTextEntered(LineEdit.LineEditEventArgs obj)
        {
            if (!string.IsNullOrEmpty(obj.Text))
            {
                SendMessage(new PaperInputTextMessage(obj.Text));

                if (_window != null)
                {
                    _window.Input.Text = string.Empty;
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
