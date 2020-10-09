using Content.Shared.GameObjects.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects.Components.UserInterface;

namespace Content.Client.GameObjects.Components
{
    [UsedImplicitly]
    public class AcceptCloningBoundUserInterface : BoundUserInterface
    {

        public AcceptCloningBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        private AcceptCloningWindow _window;

        protected override void Open()
        {
            base.Open();

            _window = new AcceptCloningWindow();
            _window.OnClose += Close;
            _window.DenyButton.OnPressed += _ => _window.Close();
            _window.ConfirmButton.OnPressed += _ =>
            {
                SendMessage(
                    new SharedAcceptCloningComponent.UiButtonPressedMessage(
                        SharedAcceptCloningComponent.UiButton.Accept));
                _window.Close();
            };
            _window.OpenCentered();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _window?.Dispose();
            }
        }

    }
}
