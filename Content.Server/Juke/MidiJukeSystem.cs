using System;
using Content.Shared.Interaction;
using Content.Shared.Juke;
using JetBrains.Annotations;
using Robust.Server.Audio.Midi;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.Juke
{
    [UsedImplicitly]
    public class MidiJukeSystem : SharedMidiJukeSystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MidiJukeComponent, InteractHandEvent>(OnInteractHand);
        }

        private void OnInteractHand(EntityUid uid, MidiJukeComponent component, InteractHandEvent args)
        {
            if (!component.Playing)
            {
                if (component.PlaybackStatus == MidiJukePlaybackStatus.Stop)
                    OpenMidiFile(component, "testmidi.mid");
                Play(component);
            }
            else
            {
                Pause(component);
            }
        }

        public override void Update(float frameTime)
        {
            foreach (var component in ComponentManager.EntityQuery<MidiJukeComponent>(true))
            {
                if (!component.Playing) continue;
                var midiEvents = component.MidiPlayer?.TickClockAndPopEventBuffer();
                if (midiEvents == null) continue;
                var uid = component.Owner.Uid;
                RaiseNetworkEvent(new MidiJukeMidiEventsEvent(uid, midiEvents.ToArray()), Filter.Pvs(component.Owner));
            }
        }

        /// <summary>
        /// Loads a MIDI file into the component's MIDI player.
        /// </summary>
        /// <param name="component"></param>
        /// <param name="filename"></param>
        private void OpenMidiFile(MidiJukeComponent component, string filename)
        {
            component.MidiPlayer?.Dispose();
            component.MidiPlayer = VirtualMidiPlayer.FromFile(filename);
            if (component.MidiPlayer != null)
            {
                component.MidiFileName = filename;
                component.MidiPlayer.Finished += (sender, eventArgs) => OnPlaybackFinished(component); //LOW KEY WORRIED ABOUT THE MEMORY SAFETY OF THIS
            }
        }

        private void OnPlaybackFinished(MidiJukeComponent component)
        {
            component.MidiPlayer?.Dispose();
            RaiseNetworkEvent(new MidiJukePlaybackFinishedEvent(component.Owner.Uid), Filter.Pvs(component.Owner));
        }

        /// <summary>
        /// Starts playback on the juke.
        /// </summary>
        /// <param name="component"></param>
        private void Play(MidiJukeComponent component)
        {
            if (!component.Playing && component.MidiPlayer != null)
            {
                component.MidiPlayer.Start();
                component.PlaybackStatus = MidiJukePlaybackStatus.Play;
                RaiseNetworkEvent(new MidiJukePlayEvent(component.Owner.Uid), Filter.Pvs(component.Owner));
            }
        }

        /// <summary>
        /// Pauses playback, maintaining position in the file.
        /// </summary>
        /// <param name="component"></param>
        private void Pause(MidiJukeComponent component)
        {
            if (component.Playing && component.MidiPlayer != null)
            {
                component.MidiPlayer.Stop();
                component.PlaybackStatus = MidiJukePlaybackStatus.Pause;
                component.MidiPlayer.FlushEventBuffer();
                RaiseNetworkEvent(new MidiJukePauseEvent(component.Owner.Uid), Filter.Pvs(component.Owner));
            }
        }

        /// <summary>
        /// Stops playback, resetting position in the file to the beginning.
        /// </summary>
        /// <param name="component"></param>
        private void Stop(MidiJukeComponent component)
        {
            if (component.Playing && component.MidiPlayer != null)
            {
                component.MidiPlayer.Stop();
                component.PlaybackStatus = MidiJukePlaybackStatus.Stop;
                component.MidiPlayer.FlushEventBuffer();
                RaiseNetworkEvent(new MidiJukeStopEvent(component.Owner.Uid), Filter.Pvs(component.Owner));
                component.MidiPlayer.MoveToStart();
            }
        }
    }
}
