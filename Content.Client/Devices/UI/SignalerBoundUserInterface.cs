using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.Devices;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.Devices.UI
{
    [UsedImplicitly]
    public class SignalerBoundUserInterface : BoundUserInterface
    {
        private SignalerWindow? _window;

        public SignalerBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            _window = new SignalerWindow();
            _window.OpenCentered();

            _window.OnClose += Close;
            _window.OnSendSignalPressed += SendSignal;
            _window.OnFrequencyChanged += UpdateFrequency;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _window?.Dispose();
        }

        /// <summary>
        /// Update the UI state based on server-sent info
        /// </summary>
        /// <param name="state"></param>
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (_window == null || state is not SignalerBoundUserInterfaceState cast)
                return;

            _window.SetFrequency(cast.Frequency);
        }

        private void SendSignal()
        {
            SendMessage(new SignalerSendSignalMessage());
        }

        private void UpdateFrequency(int freq)
        {
            SendMessage(new SignalerUpdateFrequencyMessage(freq));
        }
    }
}
