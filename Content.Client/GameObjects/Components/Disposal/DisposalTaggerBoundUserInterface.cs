#nullable enable
using JetBrains.Annotations;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Shared.GameObjects.Components.UserInterface;
using static Content.Shared.GameObjects.Components.Disposal.SharedDisposalTaggerComponent;

namespace Content.Client.GameObjects.Components.Disposal
{
    /// <summary>
    /// Initializes a <see cref="DisposalTaggerWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public class DisposalTaggerBoundUserInterface : BoundUserInterface
    {
        private DisposalTaggerWindow? _window;

        public DisposalTaggerBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = new DisposalTaggerWindow();

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

            if (state is not DisposalTaggerUserInterfaceState cast)
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
