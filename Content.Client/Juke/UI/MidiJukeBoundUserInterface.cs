using Content.Client.Juke.UI;
using Content.Shared.Juke;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;

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
            _window.SkipButtonPressed += OnSkipButtonPressed;
            _window.LoopButtonToggled += OnLoopButtonToggled;

            _window.ItemSelected += OnItemSelected;
            SendMessage(new MidiJukeSongListRequestMessage());
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            switch (message)
            {
                case MidiJukeSongListMessage songListMsg:
                    if (_window == null) return;
                    _window.PopulateList(songListMsg.SongList);
                    break;
                case MidiJukeTimestampMessage timestampMsg:
                    if (_window == null) return;
                    var elapsed = timestampMsg.Elapsed;
                    var duration = timestampMsg.Duration;
                    if (elapsed == null || duration == null)
                    {
                        _window.EmptyTime();
                    }
                    else
                    {
                        _window.SetTime((int) elapsed, (int) duration);
                    }
                    break;
            }
        }

        private void OnItemSelected(string filename)
        {
            SendMessage(new MidiJukeSongSelectMessage(filename));
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

        private void OnSkipButtonPressed()
        {
            SendMessage(new MidiJukeSkipMessage());
        }

        private void OnLoopButtonToggled(bool status)
        {
            SendMessage(new MidiJukeLoopMessage(status));
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (_window == null || state is not MidiJukeBoundUserInterfaceState cast) return;

            _window.SetPlaybackStatus(cast.PlaybackStatus);
            _window.SetLoop(cast.Loop);
            _window.SetCurrentFilename(cast.CurrentSong);
            _window.SetCurrentSongTitle(cast.CurrentSongTitle);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _window?.Dispose();
        }
    }
}
