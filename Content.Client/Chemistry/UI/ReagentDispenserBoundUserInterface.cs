using Content.Shared.Chemistry;
using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Chemistry.UI
{
    /// <summary>
    /// Initializes a <see cref="ReagentDispenserWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public sealed class ReagentDispenserBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private ReagentDispenserWindow? _window;

        [ViewVariables]
        private ReagentDispenserBoundUserInterfaceState? _lastState;

        public ReagentDispenserBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
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

            // Setup window layout/elements
            _window = new()
            {
                Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName,
            };

            _window.OpenCentered();
            _window.OnClose += Close;

            // Setup static button actions.
            _window.EjectButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent(SharedReagentDispenser.OutputSlotName));
            _window.ClearButton.OnPressed += _ => SendMessage(new ReagentDispenserClearContainerSolutionMessage());
            _window.DispenseButton1.OnPressed += _ => SendMessage(new ReagentDispenserSetDispenseAmountMessage(ReagentDispenserDispenseAmount.U1));
            _window.DispenseButton5.OnPressed += _ => SendMessage(new ReagentDispenserSetDispenseAmountMessage(ReagentDispenserDispenseAmount.U5));
            _window.DispenseButton10.OnPressed += _ => SendMessage(new ReagentDispenserSetDispenseAmountMessage(ReagentDispenserDispenseAmount.U10));
            _window.DispenseButton15.OnPressed += _ => SendMessage(new ReagentDispenserSetDispenseAmountMessage(ReagentDispenserDispenseAmount.U15));
            _window.DispenseButton20.OnPressed += _ => SendMessage(new ReagentDispenserSetDispenseAmountMessage(ReagentDispenserDispenseAmount.U20));
            _window.DispenseButton25.OnPressed += _ => SendMessage(new ReagentDispenserSetDispenseAmountMessage(ReagentDispenserDispenseAmount.U25));
            _window.DispenseButton30.OnPressed += _ => SendMessage(new ReagentDispenserSetDispenseAmountMessage(ReagentDispenserDispenseAmount.U30));
            _window.DispenseButton50.OnPressed += _ => SendMessage(new ReagentDispenserSetDispenseAmountMessage(ReagentDispenserDispenseAmount.U50));
            _window.DispenseButton100.OnPressed += _ => SendMessage(new ReagentDispenserSetDispenseAmountMessage(ReagentDispenserDispenseAmount.U100));

            // Setup reagent button actions.
            _window.OnDispenseReagentButtonPressed += (args, button) => SendMessage(new ReagentDispenserDispenseReagentMessage(button.ReagentId));
            _window.OnDispenseReagentButtonMouseEntered += (args, button) =>
            {
                if (_lastState is not null)
                    _window.UpdateContainerInfo(_lastState);
            };
            _window.OnDispenseReagentButtonMouseExited += (args, button) =>
            {
                if (_lastState is not null)
                    _window.UpdateContainerInfo(_lastState);
            };

            _window.OnEjectJugButtonPressed += (args, button) => SendMessage(new ItemSlotButtonPressedEvent(button.ReagentId));
            _window.OnEjectJugButtonMouseEntered += (args, button) => {
                if (_lastState is not null)
                    _window.UpdateContainerInfo(_lastState);
            };
            _window.OnEjectJugButtonMouseExited += (args, button) => {
                if (_lastState is not null)
                    _window.UpdateContainerInfo(_lastState);
            };
        }

        /// <summary>
        /// Update the UI each time new state data is sent from the server.
        /// </summary>
        /// <param name="state">
        /// Data of the <see cref="ReagentDispenserComponent"/> that this UI represents.
        /// Sent from the server.
        /// </param>
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            var castState = (ReagentDispenserBoundUserInterfaceState) state;
            _lastState = castState;

            _window?.UpdateState(castState); //Update window state
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
