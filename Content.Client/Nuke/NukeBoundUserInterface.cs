using Content.Client.Traitor.Uplink;
using Content.Shared.Nuke;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Client.Nuke
{
    [UsedImplicitly]
    public class NukeBoundUserInterface : BoundUserInterface
    {
        private NukeMenu? _menu;

        public NukeBoundUserInterface([NotNull] ClientUserInterfaceComponent owner, [NotNull] object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            _menu = new NukeMenu();
            _menu.OpenCentered();
            _menu.OnClose += Close;
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (_menu == null)
                return;

            switch (state)
            {
                case NukeUiState msg:
                {
                    _menu.UpdateState(msg);
                    break;
                }
            }
        }



        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            _menu?.Close();
            _menu?.Dispose();
        }
    }
}
