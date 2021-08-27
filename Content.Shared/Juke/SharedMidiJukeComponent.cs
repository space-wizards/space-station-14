using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Juke
{
    [NetworkedComponent()]
    public abstract class SharedMidiJukeComponent : Component
    {
        public sealed override string Name => "MidiJuke";

        /// <summary>
        /// Whether the juke is currently playing a song.
        /// </summary>
        public MidiJukePlaybackStatus PlaybackStatus { get; set; } = MidiJukePlaybackStatus.Stop;

        public bool Playing => PlaybackStatus == MidiJukePlaybackStatus.Play;
    }

    public enum MidiJukePlaybackStatus
    {
        Play,
        Pause,
        Stop
    }

    [Serializable, NetSerializable]
    public sealed class MidiJukeComponentState : ComponentState
    {
        public bool Playing { get; }

        public MidiJukeComponentState(bool playing)
        {
            Playing = playing;
        }
    }
}
