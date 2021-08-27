using System;
using System.Diagnostics.Tracing;
using Content.Shared.Juke;
using JetBrains.Annotations;
using Robust.Client.Audio.Midi;
using Robust.Shared.Audio.Midi;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Content.Client.Juke
{
    [UsedImplicitly]
    public class MidiJukeSystem : SharedMidiJukeSystem
    {
        [Dependency] private readonly IMidiManager _midiManager = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<MidiJukeMidiEventsEvent>(OnMidiEvent);
            SubscribeNetworkEvent<MidiJukeStopEvent>(OnStopEvent);
            SubscribeNetworkEvent<MidiJukePlayEvent>(OnPlayEvent);
            SubscribeNetworkEvent<MidiJukePauseEvent>(OnPauseEvent);
            SubscribeNetworkEvent<MidiJukePlaybackFinishedEvent>(OnPlaybackFinishedEvent);
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
                component.Renderer?.SendMidiEvent(evt); //todo: some kind of buffering?
            }
        }
    }
}
