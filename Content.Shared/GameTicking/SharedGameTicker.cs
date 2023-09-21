using Content.Shared.Roles;
using Robust.Shared.Replays;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;

namespace Content.Shared.GameTicking
{
    public abstract class SharedGameTicker : EntitySystem
    {
        [Dependency] private readonly IReplayRecordingManager _replay = default!;

        // See ideally these would be pulled from the job definition or something.
        // But this is easier, and at least it isn't hardcoded.
        //TODO: Move these, they really belong in StationJobsSystem or a cvar.
        [ValidatePrototypeId<JobPrototype>]
        public const string FallbackOverflowJob = "Passenger";

        public const string FallbackOverflowJobName = "job-name-passenger";

        // TODO network.
        // Probably most useful for replays, round end info, and probably things like lobby menus.
        [ViewVariables]
        public int RoundId { get; protected set; }

        public override void Initialize()
        {
            base.Initialize();
            _replay.RecordingStarted += OnRecordingStart;
        }

        public override void Shutdown()
        {
            _replay.RecordingStarted -= OnRecordingStart;
        }

        private void OnRecordingStart(MappingDataNode metadata, List<object> events)
        {
            metadata["roundId"] = new ValueDataNode(RoundId.ToString());
        }
    }

    [Serializable, NetSerializable]
    public sealed class TickerJoinLobbyEvent : EntityEventArgs
    {
    }

    [Serializable, NetSerializable]
    public sealed class TickerJoinGameEvent : EntityEventArgs
    {
    }

    [Serializable, NetSerializable]
    public sealed class TickerLateJoinStatusEvent : EntityEventArgs
    {
        // TODO: Make this a replicated CVar, honestly.
        public bool Disallowed { get; }

        public TickerLateJoinStatusEvent(bool disallowed)
        {
            Disallowed = disallowed;
        }
    }


    [Serializable, NetSerializable]
    public sealed class TickerLobbyStatusEvent : EntityEventArgs
    {
        public bool IsRoundStarted { get; }
        public string? LobbySong { get; }
        public string? LobbyBackground { get; }
        public bool YouAreReady { get; }
        // UTC.
        public TimeSpan StartTime { get; }
        public TimeSpan RoundStartTimeSpan { get; }
        public bool Paused { get; }

        public TickerLobbyStatusEvent(bool isRoundStarted, string? lobbySong, string? lobbyBackground, bool youAreReady, TimeSpan startTime, TimeSpan preloadTime, TimeSpan roundStartTimeSpan, bool paused)
        {
            IsRoundStarted = isRoundStarted;
            LobbySong = lobbySong;
            LobbyBackground = lobbyBackground;
            YouAreReady = youAreReady;
            StartTime = startTime;
            RoundStartTimeSpan = roundStartTimeSpan;
            Paused = paused;
        }
    }

    [Serializable, NetSerializable]
    public sealed class TickerLobbyInfoEvent : EntityEventArgs
    {
        public string TextBlob { get; }

        public TickerLobbyInfoEvent(string textBlob)
        {
            TextBlob = textBlob;
        }
    }

    [Serializable, NetSerializable]
    public sealed class TickerLobbyCountdownEvent : EntityEventArgs
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
    public sealed class TickerJobsAvailableEvent : EntityEventArgs
    {
        /// <summary>
        /// The Status of the Player in the lobby (ready, observer, ...)
        /// </summary>
        public Dictionary<NetEntity, Dictionary<string, uint?>> JobsAvailableByStation { get; }
        public Dictionary<NetEntity, string> StationNames { get; }

        public TickerJobsAvailableEvent(Dictionary<NetEntity, string> stationNames, Dictionary<NetEntity, Dictionary<string, uint?>> jobsAvailableByStation)
        {
            StationNames = stationNames;
            JobsAvailableByStation = jobsAvailableByStation;
        }
    }

    [Serializable, NetSerializable]
    public sealed class RoundEndMessageEvent : EntityEventArgs
    {
        [Serializable, NetSerializable]
        public struct RoundEndPlayerInfo
        {
            public string PlayerOOCName;
            public string? PlayerICName;
            public string Role;
            public NetEntity? PlayerNetEntity;
            public bool Antag;
            public bool Observer;
            public bool Connected;
        }

        public string GamemodeTitle { get; }
        public string RoundEndText { get; }
        public TimeSpan RoundDuration { get; }
        public int RoundId { get; }
        public int PlayerCount { get; }
        public RoundEndPlayerInfo[] AllPlayersEndInfo { get; }
        public string? LobbySong;
        public string? RestartSound;

        public RoundEndMessageEvent(
            string gamemodeTitle,
            string roundEndText,
            TimeSpan roundDuration,
            int roundId,
            int playerCount,
            RoundEndPlayerInfo[] allPlayersEndInfo,
            string? lobbySong,
            string? restartSound)
        {
            GamemodeTitle = gamemodeTitle;
            RoundEndText = roundEndText;
            RoundDuration = roundDuration;
            RoundId = roundId;
            PlayerCount = playerCount;
            AllPlayersEndInfo = allPlayersEndInfo;
            LobbySong = lobbySong;
            RestartSound = restartSound;
        }
    }


    [Serializable, NetSerializable]
    public enum PlayerGameStatus : sbyte
    {
        NotReadyToPlay = 0,
        ReadyToPlay,
        JoinedGame,
    }
}

