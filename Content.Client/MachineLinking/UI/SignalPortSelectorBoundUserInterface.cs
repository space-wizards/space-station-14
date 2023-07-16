using Content.Shared.MachineLinking;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.MachineLinking.UI
{
    public sealed class SignalPortSelectorBoundUserInterface : BoundUserInterface
    {
        private SignalPortSelectorMenu? _menu;

        private string? _selectedTransmitterPort;
        private string? _selectedReceiverPort;

        public SignalPortSelectorBoundUserInterface([NotNull] ClientUserInterfaceComponent owner, [NotNull] Enum uiKey) : base(owner, uiKey) { }

        protected override void Open()
        {
            base.Open();

            _menu = new SignalPortSelectorMenu(this);
            _menu.OnClose += Close;
            _menu.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            switch (state)
            {
                case SignalPortsState data:
                    _menu?.UpdateState(data);
                    _selectedTransmitterPort = null;
                    _selectedReceiverPort = null;
                    break;
            }
        }

        public void OnTransmitterPortSelected(string port)
        {
            _selectedTransmitterPort = port;
            if (_selectedReceiverPort != null)
            {
                SendMessage(new SignalPortSelected(_selectedTransmitterPort, _selectedReceiverPort));
                _selectedTransmitterPort = null;
                _selectedReceiverPort = null;
            }
        }

        public void OnReceiverPortSelected(string port)
        {
            _selectedReceiverPort = port;
            if (_selectedTransmitterPort != null)
            {
                SendMessage(new SignalPortSelected(_selectedTransmitterPort, _selectedReceiverPort));
                _selectedTransmitterPort = null;
                _selectedReceiverPort = null;
            }
        }

        public void OnClearPressed()
        {
            _selectedTransmitterPort = null;
            _selectedReceiverPort = null;
            SendMessage(new LinkerClearSelected());
        }

        public void OnLinkDefaultPressed()
        {
            _selectedTransmitterPort = null;
            _selectedReceiverPort = null;
            SendMessage(new LinkerLinkDefaultSelected());
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
