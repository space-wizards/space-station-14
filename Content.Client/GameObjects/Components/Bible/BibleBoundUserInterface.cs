using Content.Shared.GameObjects.Components.Bible;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Shared.GameObjects.Components.UserInterface;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Client.GameObjects.Components.Bible
{
    public class BibleBoundUserInterface : BoundUserInterface
    {
        public BibleBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        private BibleSelectMenu _menu;

        protected override void Open()
        {
            base.Open();
            _menu = new BibleSelectMenu(this);

            _menu.OnClose += Close;
            _menu.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            _menu.Populate((BibleBoundUserInterfaceState) state);
        }

        public void SelectStyle(string style)
        {
            SendMessage(new BibleSelectStyleMessage(style));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            _menu?.Dispose();
        }
    }
}
