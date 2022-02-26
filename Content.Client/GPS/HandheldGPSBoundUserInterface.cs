using Robust.Client.GameObjects;
using Content.Shared.GPS;

namespace Content.Client.GPS.UI
{
    public sealed class HandheldGPSBoundUserInterface : BoundUserInterface
    {
        [ViewVariables] private HandheldGPSMenu? _menu;

        public HandheldGPSBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            var entMan = IoCManager.Resolve<IEntityManager>();

            _menu = new HandheldGPSMenu(this, Owner.Owner) { Title = Loc.GetString("handheld-gps-ui-title")};

            _menu.OnClose += Close;
            _menu.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (_menu == null)
            {
                return;
            }

            if (state is not UpdateGPSLocationState cast)
                return;

            _menu.Populate(cast);
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
