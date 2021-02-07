using Content.Shared.GameObjects.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects.Components.UserInterface;

namespace Content.Client.GameObjects.Components.Paper
{
    [UsedImplicitly]
    public class PaperBoundUserInterface : BoundUserInterface
    {
        private PaperWindow _window;

        public PaperBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();
            _window = new PaperWindow
            {
                Title = Owner.Owner.Name,
            };
            _window.OnClose += Close;
            _window.Input.OnTextEntered += Input_OnTextEntered;
            _window.OpenCentered();

        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            _window.Populate((SharedPaperComponent.PaperBoundUserInterfaceState)state);
        }

        private void Input_OnTextEntered(LineEdit.LineEditEventArgs obj)
        {
            if(!string.IsNullOrEmpty(obj.Text))
            {
                SendMessage(new SharedPaperComponent.PaperInputText(obj.Text));
                _window.Input.Text = string.Empty;
            }
        }
    }
}
