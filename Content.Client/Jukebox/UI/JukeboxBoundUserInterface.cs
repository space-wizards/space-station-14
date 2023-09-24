using Content.Shared.Jukebox;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client.Jukebox.UI
{
    public sealed class JukeboxBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private JukeboxMenu? _menu;
        [ViewVariables]
        private JukeboxSystem? _system;

        [ViewVariables]
        private List<MusicListDefinition> _cachedList = new();

        public JukeboxComponent? Jukebox { get; private set; }

        [Dependency] public readonly IGameTiming _timing = default!;

        public JukeboxBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
            IoCManager.InjectDependencies(this);
        }

        protected override void Open()
        {
            base.Open();

            if (!EntMan.TryGetComponent(Owner, out JukeboxComponent? jukebox))
                return;

            Jukebox = jukebox;

            _menu = new JukeboxMenu(this);

            _menu.OnClose += Close;

            _menu.OpenCentered();

            _system = EntMan.System<JukeboxSystem>();

            _cachedList = _system.GetList(Owner);
            _menu.SetPlayPauseButton(Jukebox.Playing);
            _menu.SetSelectedSong(Jukebox.JukeboxMusicCollection.Songs[Jukebox.SelectedSongID]);

            _menu.Populate(_cachedList);
        }

        public void TogglePlaying()
        {
        SendMessage(new JukeboxPlayingMessage());

            //var playing = !jukeboxComp.Playing;

            if(_menu == null)
                return;
            
            if (Jukebox is null)
                return;
            _menu.SetPlayPauseButton(!Jukebox.Playing);
        }

        public void Stop()
        {
        SendMessage(new JukeboxStopMessage());
        }

        public void SelectSong(int songid)
        {
        SendMessage(new JukeboxSelectedMessage(songid));

            if (Jukebox == null)
                return;

            if(_menu == null)
                return;

            _menu.SetSelectedSong(Jukebox.JukeboxMusicCollection.Songs[songid]);
        }

        public void SetTime(float time)
        {
        SendMessage(new JukeboxSetTimeMessage(time));
        }
        public void CLSetTime(float time)
        {
            if (Jukebox is null || _system is null)
                return;
            _system.setTime(Jukebox, time);
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not JukeboxBoundUserInterfaceState newState)
                return;

            if (Jukebox == null)
                return;
            
            if(_menu == null)
                return;

            _menu.SetPlayPauseButton(newState.Playing);
            _menu.SetSelectedSong(Jukebox.JukeboxMusicCollection.Songs[newState.SelectedSongID]);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            if (_menu == null)
                return;

            _menu.OnClose -= Close;
            _menu.Dispose();
        }
    }
}
