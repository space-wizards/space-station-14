using Content.Shared.Chemistry.Dispenser;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using static Content.Shared.AME.SharedAMEControllerComponent;

namespace Content.Client.AME.UI
{
    [UsedImplicitly]
    public sealed class AMEControllerBoundUserInterface : BoundUserInterface
    {
        private AMEWindow? _window;

        public AMEControllerBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = new AMEWindow(this);
            _window.OnClose += Close;
            _window.OpenCentered();
        }

        /// <summary>
        /// Update the ui each time new state data is sent from the server.
        /// </summary>
        /// <param name="state">
        /// Data of the <see cref="SharedReagentDispenserComponent"/> that this ui represents.
        /// Sent from the server.
        /// </param>
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            var castState = (AMEControllerBoundUserInterfaceState) state;
            _window?.UpdateState(castState); //Update window state
        }

        public void ButtonPressed(UiButton button, int dispenseIndex = -1)
        {
            SendMessage(new UiButtonPressedMessage(button));
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
