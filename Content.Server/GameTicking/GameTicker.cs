using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Database;
using Content.Server.Ghost;
using Content.Server.Maps;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Preferences.Managers;
using Content.Server.ServerUpdates;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Roles;
using Robust.Server;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
#if EXCEPTION_TOLERANCE
using Robust.Shared.Exceptions;
#endif

namespace Content.Server.GameTicking
{
    public sealed partial class GameTicker : SharedGameTicker
    {
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly IBanManager _banManager = default!;
        [Dependency] private readonly IBaseServer _baseServer = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly IConsoleHost _consoleHost = default!;
        [Dependency] private readonly IGameMapManager _gameMapManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly ILogManager _logManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
#if EXCEPTION_TOLERANCE
        [Dependency] private readonly IRuntimeLog _runtimeLog = default!;
#endif
        [Dependency] private readonly IServerPreferencesManager _prefsManager = default!;
        [Dependency] private readonly IServerDbManager _db = default!;
        [Dependency] private readonly ArrivalsSystem _arrivals = default!;
        [Dependency] private readonly ChatSystem _chatSystem = default!;
        [Dependency] private readonly DamageableSystem _damageable = default!;
        [Dependency] private readonly MapLoaderSystem _map = default!;
        [Dependency] private readonly GhostSystem _ghost = default!;
        [Dependency] private readonly SharedMindSystem _mind = default!;
        [Dependency] private readonly MobStateSystem _mobState = default!;
        [Dependency] private readonly PlayTimeTrackingSystem _playTimeTrackings = default!;
        [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;
        [Dependency] private readonly ServerUpdateManager _serverUpdates = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly StationJobsSystem _stationJobs = default!;
        [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly UserDbDataManager _userDb = default!;
        [Dependency] private readonly MetaDataSystem _metaData = default!;
        [Dependency] private readonly SharedRoleSystem _roles = default!;

        [ViewVariables] private bool _initialized;
        [ViewVariables] private bool _postInitialized;

        [ViewVariables] public MapId DefaultMap { get; private set; }

        private ISawmill _sawmill = default!;

        public override void Initialize()
        {
            base.Initialize();

            DebugTools.Assert(!_initialized);
            DebugTools.Assert(!_postInitialized);

            _sawmill = _logManager.GetSawmill("ticker");
            _sawmillReplays = _logManager.GetSawmill("ticker.replays");

            // Initialize the other parts of the game ticker.
            InitializeStatusShell();
            InitializeCVars();
            InitializePlayer();
            InitializeLobbyMusic();
            InitializeLobbyBackground();
            InitializeGamePreset();
            DebugTools.Assert(_prototypeManager.Index<JobPrototype>(FallbackOverflowJob).Name == FallbackOverflowJobName,
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
        }
    }
}
