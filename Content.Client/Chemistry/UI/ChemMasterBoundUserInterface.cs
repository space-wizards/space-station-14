using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Dispenser;
using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Chemistry.UI
{
    /// <summary>
    /// Initializes a <see cref="ChemMasterWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public sealed class ChemMasterBoundUserInterface : BoundUserInterface
    {
        private ChemMasterWindow? _window;

        public ChemMasterBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {

        }

        /// <summary>
        /// Called each time a chem master UI instance is opened. Generates the window and fills it with
        /// relevant info. Sets the actions for static buttons.
        /// </summary>
        protected override void Open()
        {
            base.Open();

            // Setup window layout/elements
            _window = new ChemMasterWindow
            {
                Title = Loc.GetString("chem-master-bound-user-interface-title"),
            };

            _window.OpenCentered();
            _window.OnClose += Close;

            // Setup static button actions.
            _window.EjectButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent(SharedChemMaster.ContainerSlotName));
            _window.BufferTransferButton.OnPressed += _ => SendMessage(new ChemMasterSetModeMessage(ChemMasterMode.Transfer));
            _window.BufferDiscardButton.OnPressed += _ => SendMessage(new ChemMasterSetModeMessage(ChemMasterMode.Discard));
            _window.CreatePillButton.OnPressed += _ => SendMessage(new ChemMasterCreatePillsMessage(((uint)_window.PillAmount.Value), _window.LabelLine));
            _window.CreateBottleButton.OnPressed += _ => SendMessage(new ChemMasterCreateBottlesMessage(((uint)_window.BottleAmount.Value), _window.LabelLine));

            for (uint i = 0; i < _window.PillTypeButtons.Length; i++)
            {
                var pillType = i;
                _window.PillTypeButtons[i].OnPressed += _ => SendMessage(new ChemMasterSetPillTypeMessage(pillType));
            }

            _window.OnReagentButtonPressed += (args, button) => SendMessage(new ChemMasterReagentAmountButtonMessage(button.Id, button.Amount, button.IsBuffer));
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

            var castState = (ChemMasterBoundUserInterfaceState) state;

            _window?.UpdateState(castState); // Update window state
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
