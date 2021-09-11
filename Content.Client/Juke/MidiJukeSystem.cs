using Content.Shared.Juke;
using JetBrains.Annotations;
using Robust.Client.Audio.Midi;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using MidiEvent = Robust.Shared.Audio.Midi.MidiEvent;

namespace Content.Client.Juke
{
    [UsedImplicitly]
    public class MidiJukeSystem : SharedMidiJukeSystem
    {
        [Dependency] private readonly IMidiManager _midiManager = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MidiJukeComponent, ComponentHandleState>(OnMidiJukeHandleState);
            SubscribeLocalEvent<MidiJukeComponent, ComponentInit>(OnMidiJukeInit);
            SubscribeLocalEvent<MidiJukeComponent, ComponentRemove>(OnMidiJukeRemove);

            SubscribeNetworkEvent<MidiJukeMidiEventsEvent>(OnMidiEvent);
            SubscribeNetworkEvent<MidiJukeStopEvent>(OnStopEvent);
            SubscribeNetworkEvent<MidiJukePlayEvent>(OnPlayEvent);
            SubscribeNetworkEvent<MidiJukePauseEvent>(OnPauseEvent);
            SubscribeNetworkEvent<MidiJukePlaybackFinishedEvent>(OnPlaybackFinishedEvent);
        }

        private void OnMidiJukeInit(EntityUid uid, MidiJukeComponent component, ComponentInit args)
        {
            Logger.Debug("MidiJukeInit");
            SetupRenderer(component);
        }

        private void OnMidiJukeRemove(EntityUid uid, MidiJukeComponent component, ComponentRemove args)
        {
            Logger.Debug("MidiJukeRemove");
            DisposeRenderer(component);
        }

        private void OnMidiJukeHandleState(EntityUid uid, MidiJukeComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not MidiJukeComponentState cast) return;
            Logger.DebugS("MidiJukeSystem", "Handling component state.");
            component.PlaybackStatus = cast.PlaybackStatus;
            switch (component.PlaybackStatus)
            {
                case MidiJukePlaybackStatus.Play or MidiJukePlaybackStatus.Pause when !component.IsRendererAlive:
                    //The juke is playing but our renderer is dead, so we need to start the renderer
                    SetupRenderer(component);
                    break;
                case MidiJukePlaybackStatus.Stop when component.IsRendererAlive:
                    //Reset the renderer to make sure it doesn't have some old state in it
                    ResetRenderer(component);
                    break;
            }

            // Apply any program changes we might've missed.
            // TODO: uncomment this when (if) we make midi effects not global
            // var programs = cast.ChannelPrograms;
            // for (byte channel = 0; channel < programs.Length; channel++)
            // {
            //     var old = component.ChannelPrograms[channel];
            //     component.ChannelPrograms[channel] = programs[channel];
            //     if (old != programs[channel])
            //     {
            //         component.Renderer?.SendMidiEvent(new MidiEvent
            //             { Type = 192, Channel = channel, Program = programs[channel] });
            //     }
            // }
        }

        private void OnPlaybackFinishedEvent(MidiJukePlaybackFinishedEvent evt)
        {
            var uid = evt.EntityUid;
            if (!ComponentManager.TryGetComponent<MidiJukeComponent>(uid, out var component)) return;
            if (!component.IsRendererAlive || component.Renderer == null) return;

            ResetRenderer(component);
            component.PlaybackStatus = MidiJukePlaybackStatus.Stop;
        }

        private void OnMidiEvent(MidiJukeMidiEventsEvent evt, EntitySessionEventArgs args)
        {
            var uid = evt.EntityUid;
            if (ComponentManager.TryGetComponent<MidiJukeComponent>(uid, out var component))
            {
                PlayEvents(component, evt.MidiEvents);
            }
        }

        private void OnPlayEvent(MidiJukePlayEvent evt, EntitySessionEventArgs args)
        {
            var uid = evt.EntityUid;
            if (!ComponentManager.TryGetComponent<MidiJukeComponent>(uid, out var component)) return;
            if (!component.IsRendererAlive)
                SetupRenderer(component);
            component.PlaybackStatus = MidiJukePlaybackStatus.Play;
        }

        private void OnPauseEvent(MidiJukePauseEvent evt, EntitySessionEventArgs args)
        {
            var uid = evt.EntityUid;
            if (!ComponentManager.TryGetComponent<MidiJukeComponent>(uid, out var component)) return;
            if (!component.IsRendererAlive || component.Renderer == null) return;

            component.Renderer.StopAllNotes();
            component.PlaybackStatus = MidiJukePlaybackStatus.Pause;
        }

        private void OnStopEvent(MidiJukeStopEvent evt, EntitySessionEventArgs args)
        {
            var uid = evt.EntityUid;
            if (!ComponentManager.TryGetComponent<MidiJukeComponent>(uid, out var component)) return;

            ResetRenderer(component);
            component.PlaybackStatus = MidiJukePlaybackStatus.Stop;
        }

        private void SetupRenderer(MidiJukeComponent component)
        {
            if (component.IsRendererAlive) return;
            var renderer = _midiManager.GetNewRenderer();
            if (renderer == null) return;
            renderer.DisablePercussionChannel = false;
            renderer.DisableProgramChangeEvent = false;
            renderer.TrackingEntity = component.Owner;
            component.Renderer = renderer;
        }

        private void DisposeRenderer(MidiJukeComponent component)
        {
            if (!component.IsRendererAlive || component.Renderer == null) return;
            component.Renderer.StopAllNotes();
            var renderer = component.Renderer;
            // I think this is a little ugly but instruments do it too...
            component.Owner.SpawnTimer(2000, () => {renderer?.Dispose(); });
            component.Renderer = null;
        }

        // Resetting the renderer and reusing it later is better than remaking it all the time, because
        // currently soundfont loading can take a while (like, half a second, which freezes the game interim).
        private void ResetRenderer(MidiJukeComponent component)
        {
            if (!component.IsRendererAlive) return;
            component.Renderer!.ResetSynth();
        }

        private void PlayEvents(MidiJukeComponent component, MidiEvent[] midiEvents)
        {
            if (!component.IsRendererAlive) SetupRenderer(component);
            foreach (var evt in midiEvents)
            {
                component.Renderer?.SendMidiEvent(evt); //todo: some kind of buffering?
            }
        }
    }
}
