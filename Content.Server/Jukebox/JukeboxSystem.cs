using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Jukebox;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Timing;
namespace Content.Server.Jukebox;


public sealed class JukeboxSystem : SharedJukeboxSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<JukeboxComponent, JukeboxSelectedMessage>(OnJukeboxSelected);
        SubscribeLocalEvent<JukeboxComponent, JukeboxPlayingMessage>(OnJukeboxPlay);
        SubscribeLocalEvent<JukeboxComponent, JukeboxStopMessage>(OnJukeboxStop);
        SubscribeLocalEvent<JukeboxComponent, JukeboxSetTimeMessage>(OnJukeboxSetTime);
        SubscribeLocalEvent<JukeboxComponent, ComponentShutdown>(OnComponentRemoved);

        SubscribeLocalEvent<JukeboxComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnJukeboxPlay(EntityUid uid, JukeboxComponent component, JukeboxPlayingMessage? args)
    {
        component.Playing = !component.Playing;
        if (component.Playing)
        {
            var song = component.JukeboxMusicCollection.Songs[component.SelectedSongID];
            var @params = AudioParams.Default.WithPlayOffset(component.SongTime);
            component.AudioStream = _audio.PlayPvs(song.Path, uid, @params);
            component.SongStartTime = (float) (_timing.CurTime.TotalSeconds - component.SongTime);
            component.SelectedSong = song;
        }
        else
        {
            //component.SongStartTime = (float) (_timing.CurTime.TotalSeconds - component.SongStartTime);
            if (component.AudioStream != null)
            {
                component.AudioStream.Stop();
            }
        }
        Dirty(uid, component);
        DirtyUI(uid, component);
    }

    private void OnJukeboxSetTime(EntityUid uid, JukeboxComponent component, JukeboxSetTimeMessage args)
    {
        component.SongTime = args.SongTime;
        if (component.Playing)
        {
            component.Playing = false;
            if (component.AudioStream != null)
                component.AudioStream.Stop();

            OnJukeboxPlay(uid, component, null);
        }
        Dirty(uid, component);
        DirtyUI(uid, component);
    }

    private void OnPowerChanged(EntityUid uid, JukeboxComponent component, ref PowerChangedEvent args)
    {
        TryUpdateVisualState(uid, component);

        if (!this.IsPowered(uid, EntityManager))
        {
            OnJukeboxStop(uid, component, null);
        }
    }

    private void OnJukeboxStop(EntityUid uid, JukeboxComponent component, JukeboxStopMessage? args)
    {
        component.SongTime = 0.0f;
        if (component.AudioStream != null)
        {
            component.AudioStream.Stop();
        }
        component.Playing = false;
        Dirty(uid, component);
        DirtyUI(uid, component);
    }

    private void OnJukeboxSelected(EntityUid uid, JukeboxComponent component, JukeboxSelectedMessage args)
    {
        if (!component.Playing)
        {
            component.SelectedSongID = args.Songid;
            component.SongTime = 0.0f;

            DirectSetVisualState(uid, JukeboxVisualState.Select);
            component.Selecting = true;
        }
        Dirty(uid, component);
        DirtyUI(uid, component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<JukeboxComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Selecting)
            {
                comp.SelectAccumulator += frameTime;
                if (comp.SelectAccumulator >= 0.5f)
                {
                    comp.SelectAccumulator = 0f;
                    comp.Selecting = false;

                    TryUpdateVisualState(uid, comp);
                }
            }

            if (comp.Playing)
            {
                comp.SongTime += frameTime;
                if (comp.SelectedSong == null)
                    return;
                if (comp.SongTime > comp.SelectedSong.SongLength)
                {
                    OnJukeboxStop(uid, comp, null);
                }
            }
        }
    }

    private void OnComponentRemoved(EntityUid uid, JukeboxComponent component, ComponentShutdown args)
    {
        if (component.AudioStream != null)
        {
            component.AudioStream.Stop();
        }
    }

    private void DirtyUI(EntityUid uid,
        JukeboxComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        _userInterfaceSystem.TrySetUiState(uid, JukeboxUiKey.Key,
            new JukeboxBoundUserInterfaceState(component.Playing, component.SelectedSongID));
    }

    public void DirectSetVisualState(EntityUid uid, JukeboxVisualState state)
    {
        _appearanceSystem.SetData(uid, JukeboxVisuals.VisualState, state);
    }
    public void TryUpdateVisualState(EntityUid uid, JukeboxComponent? jukeboxComponent = null)
    {
        if (!Resolve(uid, ref jukeboxComponent))
            return;

        var finalState = JukeboxVisualState.On;
        if (!this.IsPowered(uid, EntityManager))
        {
            finalState = JukeboxVisualState.Off;
        }

        _appearanceSystem.SetData(uid, JukeboxVisuals.VisualState, finalState);
    }

    protected override void OnComponentInit(EntityUid uid, JukeboxComponent component, ComponentInit args)
    {
        base.OnComponentInit(uid, component, args);

        if (HasComp<ApcPowerReceiverComponent>(uid))
        {
            TryUpdateVisualState(uid, component);
        }
    }

}
