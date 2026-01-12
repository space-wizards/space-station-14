using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Audio.Jukebox;
using Content.Shared.Power;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Audio.Jukebox;

public sealed class JukeboxSystem : SharedJukeboxSystem
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<JukeboxComponent, JukeboxSelectedMessage>(OnJukeboxSelected);
        SubscribeLocalEvent<JukeboxComponent, JukeboxPlayingMessage>(OnJukeboxPlay);
        SubscribeLocalEvent<JukeboxComponent, JukeboxPauseMessage>(OnJukeboxPause);
        SubscribeLocalEvent<JukeboxComponent, JukeboxStopMessage>(OnJukeboxStop);
        SubscribeLocalEvent<JukeboxComponent, JukeboxSetTimeMessage>(OnJukeboxSetTime);
        SubscribeLocalEvent<JukeboxComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<JukeboxComponent, ComponentShutdown>(OnComponentShutdown);

        SubscribeLocalEvent<JukeboxComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnComponentInit(Entity<JukeboxComponent> ent, ref ComponentInit args)
    {
        if (HasComp<ApcPowerReceiverComponent>(ent))
        {
            TryUpdateVisualState(ent.AsNullable());
        }
    }

    private void OnJukeboxPlay(Entity<JukeboxComponent> ent, ref JukeboxPlayingMessage args)
    {
        TryPlay(ent.AsNullable());
    }

    private void OnJukeboxPause(Entity<JukeboxComponent> ent, ref JukeboxPauseMessage args)
    {
        Pause(ent.AsNullable());
    }

    private void OnJukeboxSetTime(Entity<JukeboxComponent> ent, ref JukeboxSetTimeMessage args)
    {
        if (TryComp(args.Actor, out ActorComponent? actorComp))
        {
            var offset = actorComp.PlayerSession.Channel.Ping * 1.5f / 1000f;
            SetTime(ent.AsNullable(), args.SongTime + offset);
        }
    }

    private void OnPowerChanged(Entity<JukeboxComponent> entity, ref PowerChangedEvent args)
    {
        TryUpdateVisualState(entity.AsNullable());

        if (!this.IsPowered(entity.Owner, EntityManager))
        {
            Stop(entity.AsNullable());
        }
    }

    private void OnJukeboxStop(Entity<JukeboxComponent> entity, ref JukeboxStopMessage args)
    {
        Stop(entity.AsNullable());
    }

    private void OnJukeboxSelected(EntityUid uid, JukeboxComponent component, JukeboxSelectedMessage args)
    {
        SetSelectedTrack((uid, component), args.SongId);
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

                    TryUpdateVisualState((uid, comp));
                }
            }
        }
    }

    private void OnComponentShutdown(Entity<JukeboxComponent> ent, ref ComponentShutdown args)
    {
        ent.Comp.AudioStream = (WeakEntityReference)Audio.Stop(TryGetEntity(ent.Comp.AudioStream, out var audioEnt) ? audioEnt : null);
    }

    private void DirectSetVisualState(EntityUid uid, JukeboxVisualState state)
    {
        _appearanceSystem.SetData(uid, JukeboxVisuals.VisualState, state);
    }

    private void TryUpdateVisualState(Entity<JukeboxComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var finalState = JukeboxVisualState.On;

        if (!this.IsPowered(ent, EntityManager))
        {
            finalState = JukeboxVisualState.Off;
        }

        _appearanceSystem.SetData(ent, JukeboxVisuals.VisualState, finalState);
    }

    /// <summary>
    /// Set the selected track of the jukebox to the specified prototype.
    /// </summary>
    public void SetSelectedTrack(Entity<JukeboxComponent?> ent, ProtoId<JukeboxPrototype> track)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        TryGetEntity(ent.Comp.AudioStream, out var audioStream);
        if (!Audio.IsPlaying(audioStream))
        {
            ent.Comp.SelectedSongId = track;
            DirectSetVisualState(ent, JukeboxVisualState.Select);
            ent.Comp.Selecting = true;
            ent.Comp.AudioStream = (WeakEntityReference)Audio.Stop(audioStream);
        }

        Dirty(ent);
    }

    /// <summary>
    /// Attempts to play the jukebox's current selected track.
    /// </summary>
    /// <returns>false if no track is selected or the track prototype cannot be found, otherwise true.</returns>
    public bool TryPlay(Entity<JukeboxComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (TryGetEntity(ent.Comp.AudioStream, out var audioStream))
        {
            Audio.SetState(audioStream, AudioState.Playing);
        }
        else
        {
            Audio.Stop(audioStream);
            ent.Comp.AudioStream = new WeakEntityReference();

            if (string.IsNullOrEmpty(ent.Comp.SelectedSongId) ||
                !_protoManager.Resolve(ent.Comp.SelectedSongId, out var jukeboxProto))
            {
                return false;
            }

            ent.Comp.AudioStream = (WeakEntityReference)Audio.PlayPvs(jukeboxProto.Path, ent, AudioParams.Default.WithMaxDistance(10f))?.Entity;
            Dirty(ent);
        }
        return true;
    }

    /// <summary>
    /// Stops any track that may currently be playing.
    /// </summary>
    public void Stop(Entity<JukeboxComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        Audio.SetState(TryGetEntity(entity.Comp.AudioStream, out var audioStream) ? audioStream : null, AudioState.Stopped);
        Dirty(entity);
    }

    /// <summary>
    /// Pauses any track that may currently be playing.
    /// </summary>
    public void Pause(Entity<JukeboxComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        Audio.SetState(TryGetEntity(entity.Comp.AudioStream, out var audioStream) ? audioStream : null, AudioState.Paused);
    }

    /// <summary>
    /// Sets the playback position within the current audio track.
    /// </summary>
    /// <remarks>
    /// If setting based on user input, you may need to compensate for the player's ping.
    /// </remarks>
    public void SetTime(Entity<JukeboxComponent?> entity, float songTime)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        Audio.SetPlaybackPosition(TryGetEntity(entity.Comp.AudioStream, out var audioStream) ? audioStream : null, songTime);
    }
}
