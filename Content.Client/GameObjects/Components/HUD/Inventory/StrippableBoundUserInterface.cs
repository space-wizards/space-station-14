using Content.Client.UserInterface;
using JetBrains.Annotations;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.HUD.Inventory
{
    public class StrippableBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private StrippingMenu _strippingMenu;

        public StrippableBoundUserInterface([NotNull] ClientUserInterfaceComponent owner, [NotNull] object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _strippingMenu = new StrippingMenu();
            _strippingMenu.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
        }
    }
}
