using System;
using System.Runtime.InteropServices;
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
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<JukeboxComponent, JukeboxQueueTrackMessage>(OnJukeboxTrackQueued);
        SubscribeLocalEvent<JukeboxComponent, JukeboxDeleteRequestMessage>(OnJukeboxDeleteRequestMessage);
        SubscribeLocalEvent<JukeboxComponent, JukeboxMoveRequestMessage>(OnJukeboxMoveRequestMessage);
        SubscribeLocalEvent<JukeboxComponent, JukeboxPlayingMessage>(OnJukeboxPlay);
        SubscribeLocalEvent<JukeboxComponent, JukeboxPauseMessage>(OnJukeboxPause);
        SubscribeLocalEvent<JukeboxComponent, JukeboxStopMessage>(OnJukeboxStop);
        SubscribeLocalEvent<JukeboxComponent, JukeboxSetTimeMessage>(OnJukeboxSetTime);
        SubscribeLocalEvent<JukeboxComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<JukeboxComponent, ComponentShutdown>(OnComponentShutdown);

        SubscribeLocalEvent<JukeboxComponent, PowerChangedEvent>(OnPowerChanged);

        SubscribeLocalEvent<JukeboxMusicComponent, ComponentShutdown>(OnAudioShutdown);
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
        OnJukeboxPlay(uid, component);
    }

    private void OnJukeboxPlay(EntityUid uid, JukeboxComponent component)
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
            if (component.AudioStream != null)
            {
                AddComp<JukeboxMusicComponent>(component.AudioStream.Value);
            }

            Dirty(uid, component);
        }
    }

    private void OnJukeboxPause(Entity<JukeboxComponent> ent, ref JukeboxPauseMessage args)
    {
        Audio.SetState(ent.Comp.AudioStream, AudioState.Paused);
    }

    private void OnJukeboxSetTime(EntityUid uid, JukeboxComponent component, JukeboxSetTimeMessage args)
    {
        if (TryComp(args.Actor, out ActorComponent? actorComp))
        {
            var offset = actorComp.PlayerSession.Channel.Ping * 1.5f / 1000f;
            Audio.SetPlaybackPosition(component.AudioStream, args.SongTime + offset);
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

    private void OnJukeboxRepeat(Entity<JukeboxComponent> entity, ref JukeboxRepeatMessage args)
    {
        entity.Comp.RepeatTracks = args.Repeat;
    }

    private void OnJukeboxShuffle(Entity<JukeboxComponent> entity, ref JukeboxShuffleMessage args)
    {
        entity.Comp.ShuffleTracks = args.Shuffle;
    }

    private void OnJukeboxTrackQueued(EntityUid uid, JukeboxComponent component, JukeboxQueueTrackMessage args)
    {
        component.Queue.AddLast(args.SongId);

        if (component.SelectedSongId is null)
        {
            component.SelectedSongId = component.Queue.First!.Value;
        }

        Dirty(uid, component);
    }

        /// <summary>
        /// Removes a track from the queue by index.
        /// If the index given does not exist or is outside of the bounds of the queue, nothing happens.
        /// </summary>
        /// <param name="uid">The jukebox whose queue is being altered.</param>
        /// <param name="component"></param>
        /// <param name="args"></param>
    private void OnJukeboxDeleteRequestMessage(EntityUid uid, JukeboxComponent component, JukeboxDeleteRequestMessage args)
    {
        if (args.Index < 0 || args.Index >= component.Queue.Count)
            return;

        var node = component.Queue.First;
        for (int i = 0; i < args.Index; i++)
            node = node?.Next;

        if (node == null) // Shouldn't happen with checks above.
            return;

        var batch = node.Value;
        component.Queue.Remove(node);
        Dirty(uid, component);
    }


    private void OnJukeboxMoveRequestMessage(EntityUid uid, JukeboxComponent component, JukeboxMoveRequestMessage args)
    {
        if (args.Change == 0 || args.Index < 0 || args.Index >= component.Queue.Count)
                return;

        // New index must be within the bounds of the batch.
        var newIndex = args.Index + args.Change;
        if (newIndex < 0 || newIndex >= component.Queue.Count)
            return;

        var node = component.Queue.First;
        for (int i = 0; i < args.Index; i++)
            node = node?.Next;

        if (node == null) // Something went wrong.
            return;

        if (args.Change > 0)
        {
            var newRelativeNode = node.Next;
            for (int i = 1; i < args.Change; i++) // 1-indexed: starting from Next
                newRelativeNode = newRelativeNode?.Next;

            if (newRelativeNode == null) // Something went wrong.
                return;

            component.Queue.Remove(node);
            component.Queue.AddAfter(newRelativeNode, node);
        }
        else
        {
            var newRelativeNode = node.Previous;
            for (int i = 1; i < -args.Change; i++) // 1-indexed: starting from Previous
                newRelativeNode = newRelativeNode?.Previous;

            if (newRelativeNode == null) // Something went wrong.
                return;

            component.Queue.Remove(node);
            component.Queue.AddBefore(newRelativeNode, node);
        }

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
        }
    }

    private void OnAudioShutdown(Entity<JukeboxMusicComponent> ent, ref ComponentShutdown args)
    {
        var query = EntityQueryEnumerator<JukeboxComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.AudioStream == ent.Owner)
            {
                // Queue's empty (somehow), stop
                if (comp.Queue.Count == 0)
                {
                    Stop((uid, comp));
                    continue;
                }

                // Remove the last played song from the queue, but append it to the end if repeat is on.
                var lastPlayed = comp.Queue.First;
                comp.Queue.RemoveFirst();
                if (comp.RepeatTracks && lastPlayed is not null)
                    comp.Queue.AddLast(lastPlayed);

                // Queue's actually empty now, stop
                if (comp.Queue.Count == 0)
                {
                    Stop((uid, comp));
                    continue;
                }

                // Otherwise, pick the next song
                if (comp.ShuffleTracks)
                {
                    var nextIndex = _random.Next(comp.Queue.Count);

                    var node = comp.Queue.First;
                    for (int i = 0; i < nextIndex; i++)
                        node = node?.Next;

                    if (node == null) // Something went wrong.
                        return;

                    // since we're just playing the first song, move the shuffled song to the top, if it isn't already.
                    comp.Queue.Remove(node);
                    comp.Queue.AddFirst(node);
                }

                comp.SelectedSongId = comp.Queue.First!.Value;
                OnJukeboxPlay(uid, comp);
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
}
