using System;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using static Content.Shared.GameObjects.Components.SharedWiresComponent;

namespace Content.Client.GameObjects.Components.Wires
{
    public class WiresBoundUserInterface : BoundUserInterface
    {
#pragma warning disable 649
        [Dependency] private readonly ILocalizationManager _localizationManager;
#pragma warning restore 649
        public WiresBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        private WiresMenu _menu;

        protected override void Open()
        {
            base.Open();
            _menu = new WiresMenu(_localizationManager) {Owner = this};

            _menu.OnClose += Close;
            _menu.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            _menu.Populate((WiresBoundUserInterfaceState) state);
        }

        public void PerformAction(Guid guid, WiresAction action)
        {
            SendMessage(new WiresActionMessage(guid, action));
        }
    }
}
