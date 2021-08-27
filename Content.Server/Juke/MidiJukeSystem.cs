using System;
using Content.Server.UserInterface;
using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;
using Content.Shared.Juke;
using JetBrains.Annotations;
using Robust.Server.Audio.Midi;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Player;

namespace Content.Server.Juke
{
    [UsedImplicitly]
    public class MidiJukeSystem : SharedMidiJukeSystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MidiJukeComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<MidiJukeComponent, InteractHandEvent>(OnInteractHand);
        }

        private void OnStartup(EntityUid uid, MidiJukeComponent component, ComponentStartup args)
        {
            //TODO: this is apparently bad and will need to be rewritten once BoundUserInterface becomes ECS friendlier
            var ui = component.Owner.GetUIOrNull(MidiJukeUiKey.Key);
            if (ui != null)
            {
                ui.OnReceiveMessage += msg => OnMidiJukeUiMessage(uid, component, msg);
            }
            OpenMidiFile(component, "testmidi.mid"); //TODO: remove this placeholder
        }

        private void DirtyUI(EntityUid uid)
        {
            if (!ComponentManager.TryGetComponent(uid, out MidiJukeComponent midiJuke)
                || !ComponentManager.TryGetComponent(uid, out ServerUserInterfaceComponent userInterfaceComponent)
                || !userInterfaceComponent.TryGetBoundUserInterface(MidiJukeUiKey.Key, out var ui))
                return;

            ui.SetState(new MidiJukeBoundUserInterfaceState(midiJuke.PlaybackStatus));
        }

        private void OnMidiJukeUiMessage(EntityUid uid, MidiJukeComponent component, ServerBoundUserInterfaceMessage msg)
        {
            var entity = msg.Session.AttachedEntity;
            if (entity == null
                || !Get<ActionBlockerSystem>().CanInteract(entity)
                || !Get<ActionBlockerSystem>().CanUse(entity))
                return;

            switch (msg.Message)
            {
                case MidiJukePlayMessage:
                    Play(component);
                    break;
                case MidiJukePauseMessage:
                    Pause(component);
                    break;
                case MidiJukeStopMessage:
                    Stop(component);
                    break;
                case MidiJukeLoopMessage:
                    Logger.Debug("MidiJukeLoop"); //TODO: implement
                    break;
            }
        }

        private void OnInteractHand(EntityUid uid, MidiJukeComponent component, InteractHandEvent args)
        {
            /*if (!component.Playing)
            {
                if (component.PlaybackStatus == MidiJukePlaybackStatus.Stop)
                    OpenMidiFile(component, "testmidi.mid");
                Play(component);
            }
            else
            {
                Pause(component);
            }*/
            if (!args.User.TryGetComponent(out ActorComponent? actor)) return;
            component.Owner.GetUIOrNull(MidiJukeUiKey.Key)?.Open(actor.PlayerSession);
            args.Handled = true;
        }

        public override void Update(float frameTime)
        {
            foreach (var component in ComponentManager.EntityQuery<MidiJukeComponent>(true))
            {
                if (!component.Playing || component.MidiPlayer == null) continue;
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
            component.MidiPlayer?.MoveToStart();
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
