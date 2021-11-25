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
            _window.EjectButton.OnPressed += _ => PrepareData(UiAction.Eject, null, null, null, null);
            _window.BufferTransferButton.OnPressed += _ => PrepareData(UiAction.Transfer, null, null, null, null);
            _window.BufferDiscardButton.OnPressed += _ => PrepareData(UiAction.Discard, null, null, null, null);
            _window.CreatePillButton.OnPressed += _ => PrepareData(UiAction.CreatePills, null, null, _window.PillAmount.Value, null);
            _window.CreateBottleButton.OnPressed += _ => PrepareData(UiAction.CreateBottles, null, null, null, _window.BottleAmount.Value);

            _window.PillTypeButton1.OnPressed += _ => PrepareData(UiAction.SetPillType1, null, 1, null, null);
            _window.PillTypeButton2.OnPressed += _ => PrepareData(UiAction.SetPillType2, null, 2, null, null);
            _window.PillTypeButton3.OnPressed += _ => PrepareData(UiAction.SetPillType3, null, 3, null, null);
            _window.PillTypeButton4.OnPressed += _ => PrepareData(UiAction.SetPillType4, null, 4, null, null);
            _window.PillTypeButton5.OnPressed += _ => PrepareData(UiAction.SetPillType5, null, 5, null, null);
            _window.PillTypeButton6.OnPressed += _ => PrepareData(UiAction.SetPillType6, null, 6, null, null);
            _window.PillTypeButton7.OnPressed += _ => PrepareData(UiAction.SetPillType7, null, 7, null, null);
            _window.PillTypeButton8.OnPressed += _ => PrepareData(UiAction.SetPillType8, null, 8, null, null);
            _window.PillTypeButton9.OnPressed += _ => PrepareData(UiAction.SetPillType9, null, 9, null, null);
            _window.PillTypeButton10.OnPressed += _ => PrepareData(UiAction.SetPillType10, null, 10, null, null);
            _window.PillTypeButton11.OnPressed += _ => PrepareData(UiAction.SetPillType11, null, 11, null, null);
            _window.PillTypeButton12.OnPressed += _ => PrepareData(UiAction.SetPillType12, null, 12, null, null);
            _window.PillTypeButton13.OnPressed += _ => PrepareData(UiAction.SetPillType13, null, 13, null, null);
            _window.PillTypeButton14.OnPressed += _ => PrepareData(UiAction.SetPillType14, null, 14, null, null);
            _window.PillTypeButton15.OnPressed += _ => PrepareData(UiAction.SetPillType15, null, 15, null, null);
            _window.PillTypeButton16.OnPressed += _ => PrepareData(UiAction.SetPillType16, null, 16, null, null);
            _window.PillTypeButton17.OnPressed += _ => PrepareData(UiAction.SetPillType17, null, 17, null, null);
            _window.PillTypeButton18.OnPressed += _ => PrepareData(UiAction.SetPillType18, null, 18, null, null);
            _window.PillTypeButton19.OnPressed += _ => PrepareData(UiAction.SetPillType19, null, 19, null, null);
            _window.PillTypeButton20.OnPressed += _ => PrepareData(UiAction.SetPillType20, null, 20, null, null);

            _window.OnChemButtonPressed += (args, button) => PrepareData(UiAction.ChemButton, button, null, null, null);
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

        private void PrepareData(UiAction action, ChemButton? button, int? pillType, int? pillAmount, int? bottleAmount)
        {
            if (button != null)
            {
                SendMessage(new UiActionMessage(action, button.Amount, button.Id, button.isBuffer, null, null, null));
            }
            else
            {
                SendMessage(new UiActionMessage(action, null, null, null, pillType, pillAmount, bottleAmount));
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
