using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Database;
using Content.Server.Ghost;
using Content.Server.Maps;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Preferences.Managers;
using Content.Server.ServerUpdates;
using Content.Server.Station.Systems;
using Content.Shared.Chat;
using Content.Shared.GameTicking;
using Content.Shared.Station.Systems;
using Robust.Server;
using Robust.Server.GameStates;
using Robust.Shared.Console;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Utility;
#if EXCEPTION_TOLERANCE
using Robust.Shared.Exceptions;
#endif

namespace Content.Server.GameTicking
{
    public sealed partial class ServerGameTicker : GameTicker
    {
        [Dependency] private IBanManager _banManager = default!;
        [Dependency] private IBaseServer _baseServer = default!;
        [Dependency] private IChatManager _chatManager = default!;
        [Dependency] private IConsoleHost _consoleHost = default!;
        [Dependency] private IGameMapManager _gameMapManager = default!;
        [Dependency] private ILogManager _logManager = default!;
#if EXCEPTION_TOLERANCE
        [Dependency] private IRuntimeLog _runtimeLog = default!;
#endif
        [Dependency] private IServerPreferencesManager _prefsManager = default!;
        [Dependency] private IServerDbManager _db = default!;
        [Dependency] private ChatSystem _chatSystem = default!;
        [Dependency] private GhostSystem _ghost = default!;
        [Dependency] private MapLoaderSystem _loader = default!;
        [Dependency] private PlayTimeTrackingSystem _playTimeTrackings = default!;
        [Dependency] private PvsOverrideSystem _pvsOverride = default!;
        [Dependency] private ServerDbEntryManager _dbEntryManager = default!;
        [Dependency] private ServerUpdateManager _serverUpdates = default!;
        [Dependency] private ServerStationJobsSystem _stationJobs = default!;
        [Dependency] private StationSpawningSystem _stationSpawning = default!;
        [Dependency] private UserDbDataManager _userDb = default!;

        [ViewVariables] private bool _initialized;
        [ViewVariables] private bool _postInitialized;

        [ViewVariables] public MapId DefaultMap { get; private set; }

        private bool _randomizeCharacters;

        public override void Initialize()
        {
            base.Initialize();

            DebugTools.Assert(!_initialized);
            DebugTools.Assert(!_postInitialized);

            // TODO: Move replays to their own bespoke system so we don't need two sawmills, or allow for name overrides for specific logs
            _sawmillReplays = _logManager.GetSawmill("ticker.replays");

            // Initialize the other parts of the game ticker.
            InitializeStatusShell();
            InitializeCVars();
            InitializePlayer();
            InitializeLobbyBackground();
            InitializeGamePreset();
            DebugTools.Assert(ProtoMan.Index(FallbackOverflowJob).Name == FallbackOverflowJobName,
                "Overflow role does not have the correct name!");
            InitializeGameRules();
            InitializeReplays();
            _initialized = true;
        }

        public void PostInitialize()
        {
            DebugTools.Assert(_initialized);
            DebugTools.Assert(!_postInitialized);

            // We restart the round now that entities are initialized and prototypes have been loaded.
            if (!DummyTicker)
                RestartRound();

            _postInitialized = true;
        }

        public override void Shutdown()
        {
            base.Shutdown();

            ShutdownGameRules();
        }

        private void SendServerMessage(string message)
        {
            var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
            _chatManager.ChatMessageToAll(ChatChannel.Server, message, wrappedMessage, default, false, true);
        }

        public override void Update(float frameTime)
        {
            if (DummyTicker)
                return;
            base.Update(frameTime);
            UpdateRoundFlow(frameTime);
            UpdateGameRules();
        }

        public static int GetRoundId(IEntitySystemManager esm)
        {
            return esm.GetEntitySystemOrNull<ServerGameTicker>()?.RoundId ?? 0;
        }
    }
}
