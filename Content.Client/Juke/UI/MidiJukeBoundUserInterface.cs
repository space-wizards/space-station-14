using Content.Client.Juke.UI;
using Content.Shared.Juke;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Juke.UI
{
    [UsedImplicitly]
    public class MidiJukeBoundUserInterface : BoundUserInterface
    {
        private MidiJukeWindow? _window;

        public MidiJukeBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();
            _window = new MidiJukeWindow();

            if (State != null)
            {
                UpdateState(State);
            }

            _window.OpenCentered();

            _window.OnClose += Close;
            _window.PlayButtonPressed += OnPlayButtonPressed;
            _window.PauseButtonPressed += OnPauseButtonPressed;
            _window.StopButtonPressed += OnStopButtonPressed;
            _window.LoopButtonPressed += OnLoopButtonPressed;
        }

        private void OnPlayButtonPressed()
        {
            SendMessage(new MidiJukePlayMessage());
        }

        private void OnPauseButtonPressed()
        {
            SendMessage(new MidiJukePauseMessage());
        }

        private void OnStopButtonPressed()
        {
            SendMessage(new MidiJukeStopMessage());
        }

        private void OnLoopButtonPressed()
        {
            SendMessage(new MidiJukeLoopMessage());
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _window?.Dispose();
        }
    }
}
