using Content.Shared.Audio.Jukebox;
using Robust.Client.Audio;
using Robust.Client.Player;
using Robust.Shared.Audio.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.Audio.Jukebox;

public sealed class JukeboxBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    [ViewVariables]
    private JukeboxMenu? _menu;

    public JukeboxBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _menu = new JukeboxMenu();
        _menu.OnClose += Close;
        _menu.OpenCentered();

        _menu.OnPlayPressed += args =>
        {
            if (args)
            {
                SendMessage(new JukeboxPlayingMessage());
            }
            else
            {
                SendMessage(new JukeboxPauseMessage());
            }
        };

        _menu.OnStopPressed += () =>
        {
            SendMessage(new JukeboxStopMessage());
        };

        _menu.OnSongSelected += SelectSong;

        _menu.SetTime += SetTime;

        _menu.Populate(_protoManager.EnumeratePrototypes<JukeboxPrototype>());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not JukeboxBoundInterfaceState bState || _menu == null)
            return;

        // TODO: Need a way to sub to compstates as otherwise this can fail in some situations.
        var audio = EntMan.GetEntity(bState.Audio);
        _menu.SetAudioStream(audio);

        if (_protoManager.TryIndex(bState.SelectedSong, out var songProto))
        {
            var length = EntMan.System<AudioSystem>().GetAudioLength(songProto.Path.Path.ToString());
            _menu.SetSelectedSong(songProto.Name, (float) length.TotalSeconds);
        }
        else
        {
            _menu.SetSelectedSong(string.Empty, 0f);
        }
    }

    public void SelectSong(ProtoId<JukeboxPrototype> songid)
    {
        SendMessage(new JukeboxSelectedMessage(songid));
    }

    public void SetTime(float time)
    {
        var sentTime = time;

        // You may be wondering, what the fuck is this
        // Well we want to be able to predict the playback slider change, of which there are many ways to do it
        // We can't just use SendPredictedMessage because it will reset every tick and audio updates every frame
        // so it will go BRRRRT
        // Using ping gets us close enough that it SHOULD, MOST OF THE TIME, fall within the 0.1 second tolerance
        // that's still on engine so our playback position never gets corrected.
        if (EntMan.TryGetComponent(Owner, out JukeboxComponent? jukebox) &&
            EntMan.TryGetComponent(jukebox.AudioStream, out AudioComponent? audioComp))
        {
            var session = IoCManager.Resolve<IPlayerManager>().LocalSession;
            var ping = TimeSpan.FromMilliseconds((session?.Channel.Ping ?? 0) * 1.5);

            time = MathF.Min(time, (float) EntMan.System<AudioSystem>().GetAudioLength(audioComp.FileName).TotalSeconds);
            audioComp.PlaybackPosition = time;
            sentTime += (float) ping.TotalSeconds;
        }

        SendMessage(new JukeboxSetTimeMessage(sentTime));
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
        _menu = null;
    }
}

