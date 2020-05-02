using Robust.Client.GameObjects.Components.UserInterface;
using System;
using System.Collections.Generic;
using System.Text;
using Content.Shared.Kitchen;
using Robust.Shared.GameObjects.Components.UserInterface;

namespace Content.Client.GameObjects.Components.Kitchen
{
    public  class MicrowaveBoundUserInterface : BoundUserInterface
    {
        private MicrowaveMenu _menu;

        public MicrowaveBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner,uiKey)
        {

        }

        protected override void Open()
        {
            base.Open();
            _menu = new MicrowaveMenu(this);
            _menu.OpenCentered();
            _menu.OnClose += Close;
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (!(state is MicrowaveUserInterfaceState cstate))
                return;
            _menu.RefreshReagents(cstate.ContainedReagents);

        }

        public void Cook()
        {
            SendMessage(new SharedMicrowaveComponent.MicrowaveStartCookMessage());
        }

        public void Eject()
        {
            SendMessage(new SharedMicrowaveComponent.MicrowaveEjectMessage());
        }
    }
}
