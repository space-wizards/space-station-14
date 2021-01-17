using Content.Shared.GameObjects.Components.Disposal;
using JetBrains.Annotations;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Client.UserInterface;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Input;

namespace Content.Client.GameObjects.Components.Disposal
{
    [UsedImplicitly]
    public class DisposalTagGunBoundUserInterface : BoundUserInterface
    {
        private DisposalTagGunWindow _window;

        public DisposalTagGunBoundUserInterface([NotNull] ClientUserInterfaceComponent owner, [NotNull] object uiKey) : base(owner, uiKey)
        {
            _window = new DisposalTagGunWindow();
            _window.OpenCentered();
            _window.OnClose += Close;

            _window.LineEdit.OnKeyBindDown += OnKeyBindDown;
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            switch (message)
            {
                case SharedDisposalTagGunComponent.TagChangedMessage msg:
                    if (_window != null) _window.LineEdit.Text = msg.Tag;
                    return;
            }
        }

        private void OnKeyBindDown(GUIBoundKeyEventArgs args)
        {
            if(args.Function != EngineKeyFunctions.TextSubmit) return;

            SendMessage(new SharedDisposalTagGunComponent.TagChangedMessage(_window.LineEdit.Text));
            _window?.Close();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _window?.Dispose();
            }
        }
    }
}
