using Content.Shared.Interaction;
using JetBrains.Annotations;
using Robust.Shared.Audio.Midi;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Shared.Juke
{
    [UsedImplicitly]
    public abstract class SharedMidiJukeSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedMidiJukeComponent, ComponentHandleState>(OnMidiJukeHandleState);
        }

        private void OnMidiJukeHandleState(EntityUid uid, SharedMidiJukeComponent component, ComponentHandleState args)
        {
            throw new System.NotImplementedException();
        }
    }

    public enum MidiJukePlaybackState
    {
        Play,
        Pause,
        Stop
    }

    public class MidiJukePlaybackStateMessage : EntityEventArgs
    {
        public MidiJukePlaybackState OldState { get; }
        public MidiJukePlaybackState NewState { get; }

        public MidiJukePlaybackStateMessage(MidiJukePlaybackState oldState, MidiJukePlaybackState newState)
        {
            OldState = oldState;
            NewState = newState;
        }
    }

    public class MidiJukeMidiEventsMessage : EntityEventArgs
    {
        public MidiEvent[] MidiEvents;

        public MidiJukeMidiEventsMessage(MidiEvent[] midiEvents)
        {
            MidiEvents = midiEvents;
        }
    }
}
