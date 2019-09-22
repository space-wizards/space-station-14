using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects.Components.UserInterface;
using static Content.Shared.GameObjects.Components.Chemistry.SharedReagentDispenserComponent;

namespace Content.Client.GameObjects.Components.Chemistry
{
    public class ReagentDispenserBoundUserInterface : BoundUserInterface
    {
        private ReagentDispenserWindow _window;
        private ReagentDispenserBoundUserInterfaceState _lastState;

        public ReagentDispenserBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {

        }

        protected override void Open()
        {
            base.Open();

            _window = new ReagentDispenserWindow()
            {
                Title = "Reagent dispenser",
                Size = (500, 600)
            };
            
            _window.OpenCenteredMinSize();
            _window.OnClose += Close;

            _window.EjectButton.OnPressed += _ => ButtonPressed(UiButton.Eject);
            _window.ClearButton.OnPressed += _ => ButtonPressed(UiButton.Clear);
            _window.DispenseButton1.OnPressed += _ => ButtonPressed(UiButton.SetDispenseAmount1);
            _window.DispenseButton5.OnPressed += _ => ButtonPressed(UiButton.SetDispenseAmount5);
            _window.DispenseButton10.OnPressed += _ => ButtonPressed(UiButton.SetDispenseAmount10);
            _window.DispenseButton25.OnPressed += _ => ButtonPressed(UiButton.SetDispenseAmount25);
            _window.DispenseButton50.OnPressed += _ => ButtonPressed(UiButton.SetDispenseAmount50);
            _window.DispenseButton100.OnPressed += _ => ButtonPressed(UiButton.SetDispenseAmount100);
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            var castState = (ReagentDispenserBoundUserInterfaceState)state;
            _lastState = castState;

            _window?.UpdateState(castState);
            UpdateReagentsList(castState.Inventory);

            _window.ForceRunLayoutUpdate();
        }

        private void UpdateReagentsList(List<ReagentDispenserInventoryEntry> inventory)
        {
            _window.UpdateReagentsList(inventory);
            for (int i = 0; i < _window.ChemicalList.Children.Count(); i++)
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

        public void ButtonPressed(UiButton button, int dispenseIndex = -1)
        {
            SendMessage(new UiButtonPressedMessage(button, dispenseIndex));
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
