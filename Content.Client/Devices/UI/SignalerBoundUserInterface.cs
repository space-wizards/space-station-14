using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Devices.UI
{
    [UsedImplicitly]
    public class SignalerBoundUserInterface : BoundUserInterface
    {
        private SignalerWindow? _window;

        public SignalerBoundUserInterface([NotNull] ClientUserInterfaceComponent owner, [NotNull] object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            _window = new SignalerWindow();
            _window.OpenCentered();

            _window.OnClose += Close;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _window?.Dispose();
        }
    }
}
