using System;
using Content.Shared.Interaction;
using JetBrains.Annotations;
using Robust.Shared.Audio.Midi;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Juke
{
    [UsedImplicitly]
    public abstract class SharedMidiJukeSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            //SubscribeLocalEvent<SharedMidiJukeComponent, ComponentHandleState>(OnMidiJukeHandleState);
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

    [Serializable, NetSerializable]
    public class MidiJukeMidiEventsMessage : EntityEventArgs
    {
        public MidiEvent[] MidiEvents;
        public EntityUid EntityUid;

        public MidiJukeMidiEventsMessage(EntityUid entityUid, MidiEvent[] midiEvents)
        {
            EntityUid = entityUid;
            MidiEvents = midiEvents;
        }
    }
}
