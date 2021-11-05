using Content.Client.Traitor.Uplink;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

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
