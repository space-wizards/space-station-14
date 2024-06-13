using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Audio.Jukebox;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using JukeboxComponent = Content.Shared.Audio.Jukebox.JukeboxComponent;

namespace Content.Server.Audio.Jukebox;


public sealed class JukeboxSystem : SharedJukeboxSystem
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<JukeboxComponent, JukeboxSelectedMessage>(OnJukeboxSelected);
        SubscribeLocalEvent<JukeboxComponent, JukeboxPlayingMessage>(OnJukeboxPlay);
        SubscribeLocalEvent<JukeboxComponent, JukeboxPauseMessage>(OnJukeboxPause);
        SubscribeLocalEvent<JukeboxComponent, JukeboxStopMessage>(OnJukeboxStop);
        SubscribeLocalEvent<JukeboxComponent, JukeboxSetTimeMessage>(OnJukeboxSetTime);
        SubscribeLocalEvent<JukeboxComponent, JukeboxAddQueueMessage>(OnJukeboxAddQueue);
        SubscribeLocalEvent<JukeboxComponent, JukeboxRemoveQueueMessage>(OnJukeboxRemoveQueue);
        SubscribeLocalEvent<JukeboxComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<JukeboxComponent, ComponentShutdown>(OnComponentShutdown);

        SubscribeLocalEvent<JukeboxComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnComponentInit(EntityUid uid, JukeboxComponent component, ComponentInit args)
    {
        if (HasComp<ApcPowerReceiverComponent>(uid))
        {
            TryUpdateVisualState(uid, component);
        }
    }

    private void OnJukeboxPlay(EntityUid uid, JukeboxComponent component, ref JukeboxPlayingMessage args)
    {
        if (Exists(component.AudioStream))
        {
            Audio.SetState(component.AudioStream, AudioState.Playing);
        }
        else
        {
            component.AudioStream = Audio.Stop(component.AudioStream);

            if (string.IsNullOrEmpty(component.SelectedSongId) ||
                !_protoManager.TryIndex(component.SelectedSongId, out var jukeboxProto))
            {
                return;
            }

            component.AudioStream = Audio.PlayPvs(jukeboxProto.Path, uid, AudioParams.Default.WithMaxDistance(10f))?.Entity;

            if (_entManager.TryGetComponent(component.AudioStream, out AudioComponent? audio))
            {
                var length = _audio.GetAudioLength(audio.FileName);
                component.TimeWhenSongEnds = component.Time + length - TimeSpan.FromSeconds(audio.PlaybackPosition);
            }

            Dirty(uid, component);
        }
    }

    private void OnJukeboxAddQueue(EntityUid uid, JukeboxComponent component, JukeboxAddQueueMessage args)
    {
        if (component.SelectedSongId == null)
        {
            component.SelectedSongId = args.SongId;
        }
        else
        {
            component.SongIdQueue.Add(args.SongId);
        }

        Dirty(uid, component);
    }

    private void OnJukeboxRemoveQueue(EntityUid uid, JukeboxComponent component, JukeboxRemoveQueueMessage args)
    {
        if (args.Index < 0 || args.Index >= component.SongIdQueue.Count)
            return;

        component.SongIdQueue.RemoveAt(args.Index);

        Dirty(uid, component);
    }

    private void OnJukeboxPause(Entity<JukeboxComponent> ent, ref JukeboxPauseMessage args)
    {
        ent.Comp.TimeWhenSongEnds = null;
        Audio.SetState(ent.Comp.AudioStream, AudioState.Paused);

        Dirty(ent);
    }

    private void OnJukeboxSetTime(EntityUid uid, JukeboxComponent component, JukeboxSetTimeMessage args)
    {
        if (TryComp(args.Actor, out ActorComponent? actorComp))
        {
            var offset = actorComp.PlayerSession.Channel.Ping * 1.5f / 1000f;
            Audio.SetPlaybackPosition(component.AudioStream, args.SongTime + offset);

            if (_entManager.TryGetComponent(component.AudioStream, out AudioComponent? audio))
            {
                var length = _audio.GetAudioLength(audio.FileName);
                component.TimeWhenSongEnds = component.Time + length - TimeSpan.FromSeconds(audio.PlaybackPosition);
            }
        }
    }

    private void OnPowerChanged(Entity<JukeboxComponent> entity, ref PowerChangedEvent args)
    {
        TryUpdateVisualState(entity);

        if (!this.IsPowered(entity.Owner, EntityManager))
        {
            Stop(entity);
        }
    }

    private void OnJukeboxStop(Entity<JukeboxComponent> entity, ref JukeboxStopMessage args)
    {
        Stop(entity);
    }

    private void Stop(Entity<JukeboxComponent> entity)
    {
        entity.Comp.TimeWhenSongEnds = null;
        Audio.SetState(entity.Comp.AudioStream, AudioState.Stopped);
        Dirty(entity);
    }

    private void OnJukeboxSelected(EntityUid uid, JukeboxComponent component, JukeboxSelectedMessage args)
    {
        component.SelectedSongId = args.SongId;
        DirectSetVisualState(uid, JukeboxVisualState.Select);
        component.Selecting = true;
        component.AudioStream = Audio.Stop(component.AudioStream);

        Dirty(uid, component);
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

            if (comp.TimeWhenSongEnds == null)
                continue;

            comp.Time += TimeSpan.FromSeconds(frameTime);
            if (comp.TimeWhenSongEnds < comp.Time)
            {
                var next = SongQueueDequeue((uid, comp));
                if (next == null)
                {
                    Stop((uid, comp));
                    continue;
                }

                OnJukeboxSelected(uid, comp, new JukeboxSelectedMessage(next.Value));
                var messagePlay = new JukeboxPlayingMessage();
                OnJukeboxPlay(uid, comp, ref messagePlay);
            }
        }
    }

    private void OnComponentShutdown(EntityUid uid, JukeboxComponent component, ComponentShutdown args)
    {
        component.AudioStream = Audio.Stop(component.AudioStream);
    }

    private void DirectSetVisualState(EntityUid uid, JukeboxVisualState state)
    {
        _appearanceSystem.SetData(uid, JukeboxVisuals.VisualState, state);
    }

    private void TryUpdateVisualState(EntityUid uid, JukeboxComponent? jukeboxComponent = null)
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

    private ProtoId<JukeboxPrototype>? SongQueueDequeue(Entity<JukeboxComponent> ent)
    {
        if (ent.Comp.SongIdQueue.Count == 0)
            return null;

        var next = ent.Comp.SongIdQueue[0];
        ent.Comp.SongIdQueue.RemoveAt(0);
        ent.Comp.TimeWhenSongEnds = null;

        return next;
    }
}
