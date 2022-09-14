using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using static Content.Shared.Disposal.Components.SharedDisposalRouterComponent;

namespace Content.Client.Disposal.UI
{
    /// <summary>
    /// Initializes a <see cref="DisposalRouterWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public sealed class DisposalRouterBoundUserInterface : BoundUserInterface
    {
        private DisposalRouterWindow? _window;

        public DisposalRouterBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = new DisposalRouterWindow();

            _window.OpenCentered();
            _window.OnClose += Close;

            _window.Confirm.OnPressed += _ => ButtonPressed(UiAction.Ok, _window.TagInput.Text);
            _window.TagInput.OnTextEntered += args => ButtonPressed(UiAction.Ok, args.Text);

        }

        private void ButtonPressed(UiAction action, string tag)
        {
            SendMessage(new UiActionMessage(action, tag));
            _window?.Close();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not DisposalRouterUserInterfaceState cast)
            {
                return;
            }

            _window?.UpdateState(cast);
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
