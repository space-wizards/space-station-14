using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Shared.GameObjects.Components.UserInterface;
using System;
using System.Collections.Generic;
using System.Text;
using static Content.Shared.GameObjects.Components.SharedGasAnalyzerComponent;

namespace Content.Client.GameObjects.Components.Atmos
{
    public class GasAnalyzerBoundUserInterface : BoundUserInterface
    {
        public GasAnalyzerBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        private GasAnalyzerWindow _menu;

        protected override void Open()
        {
            base.Open();
            _menu = new GasAnalyzerWindow(this);

            _menu.OnClose += Close;
            _menu.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            _menu.Populate((GasAnalyzerBoundUserInterfaceState) state);
        }

        //TODO: add refresh action here and in shared
        /*public void PerformAction(int id, WiresAction action)
        {
            SendMessage(new WiresActionMessage(id, action));
        }*/

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _menu.Close();
        }
    }
}
