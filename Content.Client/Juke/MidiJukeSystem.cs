using System;
using System.Diagnostics.Tracing;
using Content.Shared.Juke;
using JetBrains.Annotations;
using Melanchall.DryWetMidi.Core;
using Robust.Client.Audio.Midi;
using Robust.Client.GameObjects;
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

            SubscribeNetworkEvent<MidiJukeMidiEventsEvent>(OnMidiEvent);
            SubscribeNetworkEvent<MidiJukeStopEvent>(OnStopEvent);
            SubscribeNetworkEvent<MidiJukePlayEvent>(OnPlayEvent);
            SubscribeNetworkEvent<MidiJukePauseEvent>(OnPauseEvent);
            SubscribeNetworkEvent<MidiJukePlaybackFinishedEvent>(OnPlaybackFinishedEvent);
        }

        private void OnMidiJukeHandleState(EntityUid uid, MidiJukeComponent component, ComponentHandleState args)
        {
            //TODO: check playback state here and start the renderer if needed.
            Logger.Debug("Handling midijukestate");
            if (args.Current is not MidiJukeComponentState cast) return;

            var programs = cast.ChannelPrograms;
            for (var i = 0; i < programs.Length; i++)
            {
                var old = component.ChannelPrograms[i];
                component.ChannelPrograms[i] = programs[i];
                if (old != programs[i])
                {
                    component.Renderer?.SendMidiEvent(new MidiEvent
                        { Type = 192, Channel = (byte) i, Program = programs[i] });
                }
            }
        }

        private void OnPlaybackFinishedEvent(MidiJukePlaybackFinishedEvent evt)
        {
            var uid = evt.EntityUid;
            if (!ComponentManager.TryGetComponent<MidiJukeComponent>(uid, out var component)) return;
            if (!component.IsRendererAlive || component.Renderer == null) return;

            DisposeRenderer(component);
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
            if (component.IsRendererAlive) return;

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

            DisposeRenderer(component);
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

        private void PlayEvents(MidiJukeComponent component, MidiEvent[] midiEvents)
        {
            if (!component.IsRendererAlive) SetupRenderer(component);
            foreach (var evt in midiEvents)
            {
                //Keeping this up to date here isn't that important, but might as well do it.
                if (evt.Type == 192)
                {
                    if (evt.Channel > component.ChannelPrograms.Length) return;
                    component.ChannelPrograms[evt.Channel] = evt.Program;
                }
                component.Renderer?.SendMidiEvent(evt); //todo: some kind of buffering?
            }
        }
    }
}
