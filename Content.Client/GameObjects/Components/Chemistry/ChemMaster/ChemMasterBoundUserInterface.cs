#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Chemistry;
using JetBrains.Annotations;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using static Content.Shared.GameObjects.Components.Chemistry.SharedChemMasterComponent;

namespace Content.Client.GameObjects.Components.Chemistry
{
    /// <summary>
    /// Initializes a <see cref="ChemMasterWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public class ChemMasterBoundUserInterface : BoundUserInterface
    {
#pragma warning disable 649
        [Dependency] private readonly ILocalizationManager _localizationManager;
#pragma warning restore 649

        private ChemMasterWindow _window;
        private ChemMasterBoundUserInterfaceState _lastState;

        public ChemMasterBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
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
            _window = new ChemMasterWindow
            {
                Title = _localizationManager.GetString("ChemMaster 3000"),
            };

            _window.OpenCentered();
            _window.OnClose += Close;

            //Setup static button actions.
            _window.EjectButton.OnPressed += _ => PrepareData(UiAction.Eject, null);
            _window.BufferTransferButton.OnPressed += _ => PrepareData(UiAction.Transfer, null);
            _window.BufferDiscardButton.OnPressed += _ => PrepareData(UiAction.Discard, null);
            /* _window.DispenseButton1.OnPressed += _ => ButtonPressed(UiButton.TransferAmount1);
             _window.DispenseButton5.OnPressed += _ => ButtonPressed(UiButton.TransferAmount5);
             _window.DispenseButton10.OnPressed += _ => ButtonPressed(UiButton.TransferAmount10);
             _window.DispenseButton25.OnPressed += _ => ButtonPressed(UiButton.TransferAmount25);
             _window.DispenseButtonAll.OnPressed += _ => ButtonPressed(UiButton.TransferAmountAll);*/

            _window.OnChemButtonPressed += (args, button) => PrepareData(UiAction.ChemButton, button);
            //{
            /*if (_menu.CurrentLoggedInAccount.DataBalance < listing.Price)
            {
                failPopup = new PDAMenuPopup(Loc.GetString("Insufficient funds!"));
                _userInterfaceManager.ModalRoot.AddChild(failPopup);
                failPopup.Open(UIBox2.FromDimensions(_menu.Position.X + 150, _menu.Position.Y + 60, 156, 24));
                _menu.OnClose += () =>
                {
                    failPopup.Dispose();
                };
            }

            SendMessage(new PDAUplinkBuyListingMessage(listing));*/
            //};
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

            var castState = (ChemMasterBoundUserInterfaceState)state;
            _lastState = castState;

            _window?.UpdateState(castState); //Update window state
            //UpdateReagentsList(castState.Inventory); //Update reagents list & reagent button actions
        }

        /// <summary>
        /// Update the list of reagents that this dispenser can dispense on the UI.
        /// </summary>
        /// <param name="inventory">A list of the reagents which can be dispensed.</param>
        private void UpdateReagentsList(List</*ReagentDispenserInventoryEntry*/string> inventory)
        {
            //_window.UpdateReagentsList(inventory);
            /*for (int i = 0; i < _window.ChemicalList.Children.Count(); i++)
            {
                var button = (Button)_window.ChemicalList.Children.ElementAt(i);
                var i1 = i;
                button.OnPressed += _ => ButtonPressed(UiButton.Dispense, i1);
                button.OnMouseEntered += _ =>
                {
                    if (_lastState != null)
                    {
                       // _window.UpdateContainerInfo(_lastState, inventory[i1].ID);
                    }
                };
                button.OnMouseExited += _ =>
                {
                    if (_lastState != null)
                    {
                        //_window.UpdateContainerInfo(_lastState);
                    }
                };
            }*/
        }

        /*private void ButtonPressed(UiButton button, int dispenseIndex = -1)
        {
            //SendMessage(new UiButtonPressedMessage(button, dispenseIndex));
        }*/

        private void PrepareData(UiAction action, ChemButton? button)
        {
            if (button != null)
            {
                SendMessage(new UiActionMessage(action, button.Amount, button.Id, button.isBuffer));
            }
            else
            {
                SendMessage(new UiActionMessage(action, null, null, null));
            }

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
