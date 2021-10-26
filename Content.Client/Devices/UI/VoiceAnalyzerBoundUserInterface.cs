using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.Devices;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.Devices.UI
{
    [UsedImplicitly]
    public class VoiceAnalyzerBoundUserInterface : BoundUserInterface
    {
        private VoiceAnalyzerWindow? _window;

        public VoiceAnalyzerBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            _window = new VoiceAnalyzerWindow();
            _window.OpenCentered();

            _window.OnClose += Close;

            _window.OnVAOptionSelectedEvent += SendVAModeSelection;
            _window.OnVATextButtonPressed += SendVATextUpdate;
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
            if (_window == null || state is not VoiceAnalyzerBoundUserInterfaceState cast)
                return;

        }

        private void SendVAModeSelection(int id)
        {
            SendMessage(new VoiceAnalyzerUpdateModeMessage(id));
        }

        private void SendVATextUpdate(string text)
        {
            SendMessage(new VoiceAnalyzerUpdateTextMessage(text));
        }
    }
}
