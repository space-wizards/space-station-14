
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
    public class TickerJoinLobbyEvent : EntityEventArgs
    {
    }

    [Serializable, NetSerializable]
    public class TickerJoinGameEvent : EntityEventArgs
    {
    }

    [Serializable, NetSerializable]
    public class TickerLateJoinStatusEvent : EntityEventArgs
    {
        // TODO: Make this a replicated CVar, honestly.
        public bool Disallowed { get; }

        public TickerLateJoinStatusEvent(bool disallowed)
        {
            Disallowed = disallowed;
        }
    }


    [Serializable, NetSerializable]
    public class TickerLobbyStatusEvent : EntityEventArgs
    {
        public bool IsRoundStarted { get; }
        public string? LobbySong { get; }
        public bool YouAreReady { get; }
        // UTC.
        public TimeSpan StartTime { get; }
        public bool Paused { get; }

        public TickerLobbyStatusEvent(bool isRoundStarted, string? lobbySong, bool youAreReady, TimeSpan startTime, bool paused)
        {
            IsRoundStarted = isRoundStarted;
            LobbySong = lobbySong;
            YouAreReady = youAreReady;
            StartTime = startTime;
            Paused = paused;
        }
    }

    [Serializable, NetSerializable]
    public class TickerLobbyInfoEvent : EntityEventArgs
    {
        public string TextBlob { get; }

        public TickerLobbyInfoEvent(string textBlob)
        {
            TextBlob = textBlob;
        }
    }

    [Serializable, NetSerializable]
    public class TickerLobbyCountdownEvent : EntityEventArgs
    {
        /// <summary>
        /// The game time that the game will start at.
        /// </summary>
        public TimeSpan StartTime { get; }

        /// <summary>
        /// Whether or not the countdown is paused
        /// </summary>
        public bool Paused { get; }

        public TickerLobbyCountdownEvent(TimeSpan startTime, bool paused)
        {
            StartTime = startTime;
            Paused = paused;
        }
    }

    [Serializable, NetSerializable]
    public class TickerLobbyReadyEvent : EntityEventArgs
    {
        /// <summary>
        /// The Status of the Player in the lobby (ready, observer, ...)
        /// </summary>
        public Dictionary<NetUserId, LobbyPlayerStatus> Status { get; }

        public TickerLobbyReadyEvent(Dictionary<NetUserId, LobbyPlayerStatus> status)
        {
            Status = status;
        }
    }

    [Serializable, NetSerializable]
    public class TickerJobsAvailableEvent : EntityEventArgs
    {
        /// <summary>
        /// The Status of the Player in the lobby (ready, observer, ...)
        /// </summary>
        public string[] JobsAvailable { get; }

        public TickerJobsAvailableEvent(string[] jobsAvailable)
        {
            JobsAvailable = jobsAvailable;
        }
    }

    [Serializable, NetSerializable]
    public class RoundEndMessageEvent : EntityEventArgs
    {
        [Serializable, NetSerializable]
        public struct RoundEndPlayerInfo
        {
            public string PlayerOOCName;
            public string? PlayerICName;
            public string Role;
            public bool Antag;
            public bool Observer;
            public bool Connected;
        }

        public string GamemodeTitle { get; }
        public string RoundEndText { get; }
        public TimeSpan RoundDuration { get; }
        public int PlayerCount { get; }
        public RoundEndPlayerInfo[] AllPlayersEndInfo { get; }

        public RoundEndMessageEvent(string gamemodeTitle, string roundEndText, TimeSpan roundDuration, int playerCount,
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

