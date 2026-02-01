using System;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Audio.Jukebox;
using Content.Shared.Power;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using JukeboxComponent = Content.Shared.Audio.Jukebox.JukeboxComponent;

namespace Content.Server.Audio.Jukebox;


public sealed class JukeboxSystem : SharedJukeboxSystem
{
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<JukeboxComponent, JukeboxQueueTrackMessage>(OnJukeboxQueueTrackMessage);
        SubscribeLocalEvent<JukeboxComponent, JukeboxDeleteRequestMessage>(OnJukeboxDeleteRequestMessage);
        SubscribeLocalEvent<JukeboxComponent, JukeboxMoveRequestMessage>(OnJukeboxMoveRequestMessage);
        SubscribeLocalEvent<JukeboxComponent, JukeboxPlayingMessage>(OnJukeboxPlay);
        SubscribeLocalEvent<JukeboxComponent, JukeboxPauseMessage>(OnJukeboxPause);
        SubscribeLocalEvent<JukeboxComponent, JukeboxStopMessage>(OnJukeboxStop);
        SubscribeLocalEvent<JukeboxComponent, JukeboxRepeatMessage>(OnJukeboxRepeatMessage);
        SubscribeLocalEvent<JukeboxComponent, JukeboxShuffleMessage>(OnJukeboxShuffleMessage);
        SubscribeLocalEvent<JukeboxComponent, JukeboxSetTimeMessage>(OnJukeboxSetTime);
        SubscribeLocalEvent<JukeboxComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<JukeboxComponent, ComponentShutdown>(OnComponentShutdown);

        SubscribeLocalEvent<JukeboxComponent, PowerChangedEvent>(OnPowerChanged);

        SubscribeLocalEvent<JukeboxMusicComponent, ComponentShutdown>(OnAudioShutdown);
    }

    private void OnComponentInit(Entity<JukeboxComponent> ent, ref ComponentInit args)
    {
        if (HasComp<ApcPowerReceiverComponent>(ent.Owner))
        {
            TryUpdateVisualState(ent);
        }
    }

    private void OnJukeboxPlay(Entity<JukeboxComponent> ent, ref JukeboxPlayingMessage args)
    {
        Play(ent);
    }

    private void Play(Entity<JukeboxComponent> ent)
    {
        if (Exists(ent.Comp.AudioStream))
        {
            Audio.SetState(ent.Comp.AudioStream, AudioState.Playing);
        }
        else
        {
            ent.Comp.AudioStream = Audio.Stop(ent.Comp.AudioStream);

            if (string.IsNullOrEmpty(ent.Comp.SelectedSongId) ||
                !_protoManager.TryIndex(ent.Comp.SelectedSongId, out var jukeboxProto))
            {
                return;
            }

            ent.Comp.AudioStream = Audio.PlayPvs(jukeboxProto.Path, ent.Owner, AudioParams.Default.WithMaxDistance(10f))?.Entity;
            if (ent.Comp.AudioStream != null)
            {
                AddComp<JukeboxMusicComponent>(ent.Comp.AudioStream.Value);
            }

            Dirty(ent);
        }
    }

    private void OnJukeboxPause(Entity<JukeboxComponent> ent, ref JukeboxPauseMessage args)
    {
        Audio.SetState(ent.Comp.AudioStream, AudioState.Paused);
    }

    private void OnJukeboxSetTime(Entity<JukeboxComponent> ent, ref JukeboxSetTimeMessage args)
    {
        if (TryComp(args.Actor, out ActorComponent? actorComp))
        {
            var offset = actorComp.PlayerSession.Channel.Ping * 1.5f / 1000f;
            Audio.SetPlaybackPosition(ent.Comp.AudioStream, args.SongTime + offset);
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
        Audio.SetState(entity.Comp.AudioStream, AudioState.Stopped);
        Dirty(entity);
    }

    private void OnJukeboxRepeatMessage(Entity<JukeboxComponent> entity, ref JukeboxRepeatMessage args)
    {
        entity.Comp.RepeatTracks = args.Repeat;
        Dirty(entity);
    }

    private void OnJukeboxShuffleMessage(Entity<JukeboxComponent> entity, ref JukeboxShuffleMessage args)
    {
        entity.Comp.ShuffleTracks = args.Shuffle;
        Dirty(entity);
    }

    private void OnJukeboxQueueTrackMessage(Entity<JukeboxComponent> ent, ref JukeboxQueueTrackMessage args)
    {
        if (ent.Comp.SelectedSongId is null)
        {
            ent.Comp.SelectedSongId = args.SongId;
        }
        else
        {
            ent.Comp.Queue.Add(args.SongId);
        }

        Dirty(ent);
    }

    private void OnJukeboxDeleteRequestMessage(Entity<JukeboxComponent> ent, ref JukeboxDeleteRequestMessage args)
    {
        if (args.Index < 0 || args.Index >= ent.Comp.Queue.Count)
            return;

        ent.Comp.Queue.RemoveAt(args.Index);
        Dirty(ent);
    }


    private void OnJukeboxMoveRequestMessage(Entity<JukeboxComponent> ent, ref JukeboxMoveRequestMessage args)
    {
        if (args.Change == 0 || args.Index < 0 || args.Index >= ent.Comp.Queue.Count)
            return;

        // New index must be within the bounds of the queue.
        var newIndex = args.Index + args.Change;
        if (newIndex < 0 || newIndex >= ent.Comp.Queue.Count)
            return;

        // Only moving by 1, use a swap
        if (Math.Abs(args.Change) == 1)
        {
            var temp = ent.Comp.Queue[newIndex];
            ent.Comp.Queue[newIndex] = ent.Comp.Queue[args.Index];
            ent.Comp.Queue[args.Index] = temp;
        }
        else
        {
            var track = ent.Comp.Queue[args.Index];
            ent.Comp.Queue.RemoveAt(args.Index);

            // since we change the indices of all elements after the removed item, we have to adjust newIndex accordingly
            if (args.Change < 0)
            {
                ent.Comp.Queue.Insert(newIndex, track);
            }
            else
            {
                ent.Comp.Queue.Insert(newIndex - 1, track);
            }
        }

        Dirty(ent);
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

    private void OnAudioShutdown(Entity<JukeboxMusicComponent> ent, ref ComponentShutdown args)
    {
        var query = EntityQueryEnumerator<JukeboxComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.AudioStream == ent.Owner)
            {
                // Append last played song to the end of the queue if repeat is on.
                if (comp.RepeatTracks && comp.SelectedSongId is not null)
                    comp.Queue.Add(comp.SelectedSongId.Value);

                // Queue's empty, stop
                if (comp.Queue.Count == 0)
                {
                    Stop((uid, comp));
                    continue;
                }

                // Otherwise, pick the next song
                int nextIndex = 0;
                if (comp.ShuffleTracks)
                {
                    nextIndex = _random.Next(comp.Queue.Count);
                }

                comp.SelectedSongId = comp.Queue[nextIndex];
                comp.Queue.RemoveAt(nextIndex);

                comp.AudioStream = null; // Nuke the audio stream so that Play doesn't try and set the state of a shutting down audio stream to playing
                Play((uid, comp));
            }
        }
    }

    private void OnComponentShutdown(Entity<JukeboxComponent> ent, ref ComponentShutdown args)
    {
        ent.Comp.AudioStream = Audio.Stop(ent.Comp.AudioStream);
    }

    private void DirectSetVisualState(EntityUid uid, JukeboxVisualState state)
    {
        _appearanceSystem.SetData(uid, JukeboxVisuals.VisualState, state);
    }

    private void TryUpdateVisualState(Entity<JukeboxComponent> ent)
    {
        var finalState = JukeboxVisualState.On;

        if (!this.IsPowered(ent.Owner, EntityManager))
        {
            finalState = JukeboxVisualState.Off;
        }

        _appearanceSystem.SetData(ent.Owner, JukeboxVisuals.VisualState, finalState);
    }
}
