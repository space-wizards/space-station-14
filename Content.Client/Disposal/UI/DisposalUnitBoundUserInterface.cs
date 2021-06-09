using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using static Content.Shared.Disposal.Components.SharedDisposalUnitComponent;

namespace Content.Client.Disposal.UI
{
    /// <summary>
    /// Initializes a <see cref="DisposalUnitWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public class DisposalUnitBoundUserInterface : BoundUserInterface
    {
        private DisposalUnitWindow? _window;

        public DisposalUnitBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        private void ButtonPressed(UiButton button)
        {
            SendMessage(new UiButtonPressedMessage(button));
        }

        protected override void Open()
        {
            base.Open();

            _window = new DisposalUnitWindow();

            _window.OpenCentered();
            _window.OnClose += Close;

            _window.Eject.OnPressed += _ => ButtonPressed(UiButton.Eject);
            _window.Engage.OnPressed += _ => ButtonPressed(UiButton.Engage);
            _window.Power.OnPressed += _ => ButtonPressed(UiButton.Power);
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not DisposalUnitBoundUserInterfaceState cast)
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
