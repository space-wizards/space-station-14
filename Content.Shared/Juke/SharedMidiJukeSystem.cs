using System;
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

            SubscribeLocalEvent<SharedMidiJukeComponent, ComponentGetState>(OnMidiJukeGetState);
        }

        private void OnMidiJukeGetState(EntityUid uid, SharedMidiJukeComponent component, ref ComponentGetState args)
        {
            args.State = new MidiJukeComponentState(component.PlaybackStatus, component.ChannelPrograms);
        }
    }

    [Serializable, NetSerializable]
    public class MidiJukePlayEvent : EntityEventArgs
    {
        public EntityUid EntityUid;
        public string SongTitle;
        public MidiJukePlayEvent(EntityUid entityUid, string songTitle)
        {
            EntityUid = entityUid;
            SongTitle = songTitle;
        }
    }

    [Serializable, NetSerializable]
    public class MidiJukePauseEvent : EntityEventArgs
    {
        public EntityUid EntityUid;
        public MidiJukePauseEvent(EntityUid entityUid)
        {
            EntityUid = entityUid;
        }
    }

    [Serializable, NetSerializable]
    public class MidiJukeStopEvent : EntityEventArgs
    {
        public EntityUid EntityUid;
        public MidiJukeStopEvent(EntityUid entityUid)
        {
            EntityUid = entityUid;
        }
    }

    [Serializable, NetSerializable]
    public class MidiJukeMidiEventsEvent : EntityEventArgs
    {
        public MidiEvent[] MidiEvents;
        public EntityUid EntityUid;

        public MidiJukeMidiEventsEvent(EntityUid entityUid, MidiEvent[] midiEvents)
        {
            EntityUid = entityUid;
            MidiEvents = midiEvents;
        }
    }

    [Serializable, NetSerializable]
    public class MidiJukePlaybackFinishedEvent : EntityEventArgs
    {
        public EntityUid EntityUid;

        public MidiJukePlaybackFinishedEvent(EntityUid entityUid)
        {
            EntityUid = entityUid;
        }
    }
}
