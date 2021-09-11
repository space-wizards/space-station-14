using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

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
        /// <summary>
        /// Contains the currently selected program for each channel, so we can sync this to clients who join
        /// midway through a song.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public readonly byte[] ChannelPrograms = new byte[16];
    }

    [Serializable, NetSerializable]
    public enum MidiJukeUiKey
    {
        Key,
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
        [ViewVariables(VVAccess.ReadWrite)]
        public byte[] ChannelPrograms { get; }
        public MidiJukePlaybackStatus PlaybackStatus { get; }

        public MidiJukeComponentState(MidiJukePlaybackStatus playbackStatus, byte[] channelPrograms)
        {
            PlaybackStatus = playbackStatus;
            ChannelPrograms = channelPrograms;
        }
    }

    [Serializable, NetSerializable]
    public class MidiJukeBoundUserInterfaceState : BoundUserInterfaceState
    {
        public MidiJukePlaybackStatus PlaybackStatus { get; }
        public bool Loop { get; }
        public string CurrentSong { get; }
        public string CurrentSongTitle { get; }

        public MidiJukeBoundUserInterfaceState(MidiJukePlaybackStatus playbackStatus, bool loop, string currentSong, string currentSongTitle)
        {
            PlaybackStatus = playbackStatus;
            Loop = loop;
            CurrentSong = currentSong;
            CurrentSongTitle = currentSongTitle;
        }
    }

    [Serializable, NetSerializable]
    public class MidiJukePlayMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public class MidiJukePauseMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public class MidiJukeStopMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public class MidiJukeSkipMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public class MidiJukeLoopMessage : BoundUserInterfaceMessage
    {
        public bool Loop { get; }

        public MidiJukeLoopMessage(bool loop)
        {
            Loop = loop;
        }
    }

    [Serializable, NetSerializable]
    public class MidiJukeSongSelectMessage : BoundUserInterfaceMessage
    {
        public string Song { get; }

        public MidiJukeSongSelectMessage(string song)
        {
            Song = song;
        }
    }

    [Serializable, NetSerializable]
    public class MidiJukeSongListRequestMessage : BoundUserInterfaceMessage
    {
        public MidiJukeSongListRequestMessage()
        {
        }
    }

    [Serializable, NetSerializable]
    public class MidiJukeSongListMessage : BoundUserInterfaceMessage
    {
        public List<string> SongList { get; }

        public MidiJukeSongListMessage(List<string> songList)
        {
            SongList = songList;
        }
    }

    [Serializable, NetSerializable]
    public class MidiJukeTimestampMessage : BoundUserInterfaceMessage
    {
        public int? Elapsed { get; }
        public int? Duration { get; }

        public MidiJukeTimestampMessage(int? elapsed, int? duration)
        {
            Elapsed = elapsed;
            Duration = duration;
        }
    }

    [Serializable, NetSerializable]
    public enum MidiJukeVisualState
    {
        Base,
        Broken
    }
}
