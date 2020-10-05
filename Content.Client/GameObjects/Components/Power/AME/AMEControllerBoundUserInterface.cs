using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Shared.GameObjects.Components.UserInterface;
using static Content.Shared.GameObjects.Components.Power.AME.SharedAMEControllerComponent;

namespace Content.Client.GameObjects.Components.Power.AME
{
    public class AMEControllerBoundUserInterface : BoundUserInterface
    {
        private AMEWindow _window;

        public AMEControllerBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {

        }

        protected override void Open()
        {
            base.Open();

            _window = new AMEWindow();
            _window.OnClose += Close;
            _window.OpenCentered();

            _window.EjectButton.OnPressed += _ => ButtonPressed(UiButton.Eject);
            _window.ToggleInjection.OnPressed += _ => ButtonPressed(UiButton.ToggleInjection);
            _window.IncreaseFuelButton.OnPressed += _ => ButtonPressed(UiButton.IncreaseFuel);
            _window.DecreaseFuelButton.OnPressed += _ => ButtonPressed(UiButton.DecreaseFuel);
            _window.RefreshPartsButton.OnPressed += _ => ButtonPressed(UiButton.RefreshParts);
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

        private void ButtonPressed(UiButton button, int dispenseIndex = -1)
        {
            SendMessage(new UiButtonPressedMessage(button));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _window.Dispose();
            }
        }
    }
}
