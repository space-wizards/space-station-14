using System.Collections.Generic;
using System.Linq;
using Content.Shared.Chemistry.Dispenser;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using static Content.Shared.Chemistry.Dispenser.SharedReagentDispenserComponent;

namespace Content.Client.Chemistry.UI
{
    /// <summary>
    /// Initializes a <see cref="ReagentDispenserWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public class ReagentDispenserBoundUserInterface : BoundUserInterface
    {
        private ReagentDispenserWindow? _window;
        private ReagentDispenserBoundUserInterfaceState? _lastState;

        public ReagentDispenserBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        /// <summary>
        /// Called each time a dispenser UI instance is opened. Generates the dispenser window and fills it with
        /// relevant info. Sets the actions for static buttons.
        /// <para>Buttons which can change like reagent dispense buttons have their actions set in <see cref="UpdateReagentsList"/>.</para>
        /// </summary>
        protected override void Open()
        {
            base.Open();

            //Setup window layout/elements
            _window = new();

            _window.OpenCentered();
            _window.OnClose += Close;

            //Setup static button actions.
            _window.EjectButton.OnPressed += _ => ButtonPressed(UiButton.Eject);
            _window.ClearButton.OnPressed += _ => ButtonPressed(UiButton.Clear);
            _window.DispenseButton1.OnPressed += _ => ButtonPressed(UiButton.SetDispenseAmount1);
            _window.DispenseButton5.OnPressed += _ => ButtonPressed(UiButton.SetDispenseAmount5);
            _window.DispenseButton10.OnPressed += _ => ButtonPressed(UiButton.SetDispenseAmount10);
            _window.DispenseButton15.OnPressed += _ => ButtonPressed(UiButton.SetDispenseAmount15);
            _window.DispenseButton20.OnPressed += _ => ButtonPressed(UiButton.SetDispenseAmount20);
            _window.DispenseButton25.OnPressed += _ => ButtonPressed(UiButton.SetDispenseAmount25);
            _window.DispenseButton30.OnPressed += _ => ButtonPressed(UiButton.SetDispenseAmount30);
            _window.DispenseButton50.OnPressed += _ => ButtonPressed(UiButton.SetDispenseAmount50);
            _window.DispenseButton100.OnPressed += _ => ButtonPressed(UiButton.SetDispenseAmount100);
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

            var castState = (ReagentDispenserBoundUserInterfaceState)state;
            _lastState = castState;

            UpdateReagentsList(castState.Inventory); //Update reagents list & reagent button actions
            _window?.UpdateState(castState); //Update window state
        }

        /// <summary>
        /// Update the list of reagents that this dispenser can dispense on the UI.
        /// </summary>
        /// <param name="inventory">A list of the reagents which can be dispensed.</param>
        private void UpdateReagentsList(List<ReagentDispenserInventoryEntry> inventory)
        {
            if (_window == null)
            {
                return;
            }

            _window.UpdateReagentsList(inventory);

            for (var i = 0; i < _window.ChemicalList.Children.Count(); i++)
            {
                var button = (Button)_window.ChemicalList.Children.ElementAt(i);
                var i1 = i;
                button.OnPressed += _ => ButtonPressed(UiButton.Dispense, i1);
                button.OnMouseEntered += _ =>
                {
                    if (_lastState != null)
                    {
                        _window.UpdateContainerInfo(_lastState, inventory[i1].ID);
                    }
                };
                button.OnMouseExited += _ =>
                {
                    if (_lastState != null)
                    {
                        _window.UpdateContainerInfo(_lastState);
                    }
                };
            }
        }

        private void ButtonPressed(UiButton button, int dispenseIndex = -1)
        {
            SendMessage(new UiButtonPressedMessage(button, dispenseIndex));
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
