#nullable enable

using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.GameTicking
{
    public abstract class SharedGameTicker : EntitySystem
    {
        // See ideally these would be pulled from the job definition or something.
        // But this is easier, and at least it isn't hardcoded.
        public const string OverflowJob = "Assistant";
        public const string OverflowJobName = "assistant";
    }

    [Serializable, NetSerializable]
    public class MsgTickerJoinLobby : EntityEventArgs
    {
    }

    [Serializable, NetSerializable]
    public class MsgTickerJoinGame : EntityEventArgs
    {
    }

    [Serializable, NetSerializable]
    public class MsgTickerLateJoinStatus : EntityEventArgs
    {
        // TODO: Make this a replicated CVar, honestly.
        public bool Disallowed { get; }

        public MsgTickerLateJoinStatus(bool disallowed)
        {
            Disallowed = disallowed;
        }
    }


    [Serializable, NetSerializable]
    public class MsgTickerLobbyStatus : EntityEventArgs
    {
        public bool IsRoundStarted { get; }
        public string? LobbySong { get; }
        public bool YouAreReady { get; }
        // UTC.
        public TimeSpan StartTime { get; }
        public bool Paused { get; }

        public MsgTickerLobbyStatus(bool isRoundStarted, string? lobbySong, bool youAreReady, TimeSpan startTime, bool paused)
        {
            IsRoundStarted = isRoundStarted;
            LobbySong = lobbySong;
            YouAreReady = youAreReady;
            StartTime = startTime;
            Paused = paused;
        }
    }

    [Serializable, NetSerializable]
    public class MsgTickerLobbyInfo : EntityEventArgs
    {
        public string TextBlob { get; }

        public MsgTickerLobbyInfo(string textBlob)
        {
            TextBlob = textBlob;
        }
    }

    [Serializable, NetSerializable]
    public class MsgTickerLobbyCountdown : EntityEventArgs
    {
        /// <summary>
        /// The game time that the game will start at.
        /// </summary>
        public TimeSpan StartTime { get; }

        /// <summary>
        /// Whether or not the countdown is paused
        /// </summary>
        public bool Paused { get; }

        public MsgTickerLobbyCountdown(TimeSpan startTime, bool paused)
        {
            StartTime = startTime;
            Paused = paused;
        }
    }

    [Serializable, NetSerializable]
    public class MsgTickerLobbyReady : EntityEventArgs
    {
        /// <summary>
        /// The Status of the Player in the lobby (ready, observer, ...)
        /// </summary>
        public Dictionary<NetUserId, LobbyPlayerStatus> Status { get; }

        public MsgTickerLobbyReady(Dictionary<NetUserId, LobbyPlayerStatus> status)
        {
            Status = status;
        }
    }

    [Serializable, NetSerializable]
    public class MsgTickerJobsAvailable : EntityEventArgs
    {
        /// <summary>
        /// The Status of the Player in the lobby (ready, observer, ...)
        /// </summary>
        public string[] JobsAvailable { get; }

        public MsgTickerJobsAvailable(string[] jobsAvailable)
        {
            JobsAvailable = jobsAvailable;
        }
    }

    [Serializable, NetSerializable]
    public class MsgRoundEndMessage : EntityEventArgs
    {
        [Serializable, NetSerializable]
        public struct RoundEndPlayerInfo
        {
            public string PlayerOOCName;
            public string? PlayerICName;
            public string Role;
            public bool Antag;
            public bool Observer;
        }

        public string GamemodeTitle { get; }
        public string RoundEndText { get; }
        public TimeSpan RoundDuration { get; }
        public int PlayerCount { get; }
        public RoundEndPlayerInfo[] AllPlayersEndInfo { get; }

        public MsgRoundEndMessage(string gamemodeTitle, string roundEndText, TimeSpan roundDuration, int playerCount,
            RoundEndPlayerInfo[] allPlayersEndInfo)
        {
            GamemodeTitle = gamemodeTitle;
            RoundEndText = roundEndText;
            RoundDuration = roundDuration;
            PlayerCount = playerCount;
            AllPlayersEndInfo = allPlayersEndInfo;
        }
    }


    [Serializable, NetSerializable]
    public enum LobbyPlayerStatus : sbyte
    {
        NotReady = 0,
        Ready,
        Observer,
    }
}

