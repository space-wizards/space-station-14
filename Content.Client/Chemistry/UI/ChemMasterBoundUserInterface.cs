using Content.Shared.Chemistry.Dispenser;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using static Content.Shared.Chemistry.Components.SharedChemMasterComponent;

namespace Content.Client.Chemistry.UI
{
    /// <summary>
    /// Initializes a <see cref="ChemMasterWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public class ChemMasterBoundUserInterface : BoundUserInterface
    {
        private ChemMasterWindow? _window;

        public ChemMasterBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {

        }

        /// <summary>
        /// Called each time a chem master UI instance is opened. Generates the window and fills it with
        /// relevant info. Sets the actions for static buttons.
        /// </summary>
        protected override void Open()
        {
            base.Open();

            //Setup window layout/elements
            _window = new ChemMasterWindow
            {
                Title = Loc.GetString("chem-master-bound-user-interface-title"),
            };

            _window.OpenCentered();
            _window.OnClose += Close;

            //Setup static button actions.
            _window.EjectButton.OnPressed += _ => PrepareData(UiAction.Eject, null, null, null);
            _window.BufferTransferButton.OnPressed += _ => PrepareData(UiAction.Transfer, null, null, null);
            _window.BufferDiscardButton.OnPressed += _ => PrepareData(UiAction.Discard, null, null, null);
            _window.CreatePillButton.OnPressed += _ => PrepareData(UiAction.CreatePills, null, _window.PillAmount.Value, null);
            _window.CreateBottleButton.OnPressed += _ => PrepareData(UiAction.CreateBottles, null, null, _window.BottleAmount.Value);

            _window.OnChemButtonPressed += (args, button) => PrepareData(UiAction.ChemButton, button, null, null);
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

            _window?.UpdateState(castState); //Update window state
        }

        private void PrepareData(UiAction action, ChemButton? button, int? pillAmount, int? bottleAmount)
        {
            if (button != null)
            {
                SendMessage(new UiActionMessage(action, button.Amount, button.Id, button.isBuffer, null, null));
            }
            else
            {
                SendMessage(new UiActionMessage(action, null, null, null, pillAmount, bottleAmount));
            }
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
