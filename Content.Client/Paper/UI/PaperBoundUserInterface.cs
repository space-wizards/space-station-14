using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Content.Client.Paper;
using static Content.Shared.Paper.SharedPaperComponent;

namespace Content.Client.Paper.UI
{
    [UsedImplicitly]
    public sealed class PaperBoundUserInterface : BoundUserInterface
    {
        private PaperWindow? _window;

        public PaperBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();
            bool hugeUI = IoCManager.Resolve<IEntityManager>().GetComponent<PaperComponent>(Owner.Owner).HugeUI;
            var uiSize = (300, 300);
            if (hugeUI)
                uiSize = (1000, 700);
            _window = new PaperWindow
            {
                MinSize = uiSize,
                SetSize = uiSize,
                Title = IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(Owner.Owner).EntityName,
            };
            _window.OnClose += Close;
            _window.Input.OnTextEntered += Input_OnTextEntered;
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
                SendMessage(new PaperInputText(obj.Text));

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
