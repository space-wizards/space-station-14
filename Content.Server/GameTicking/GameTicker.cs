using Content.Server.Administration.Logs;
using Content.Server.CharacterAppearance.Systems;
using Content.Server.Chat.Managers;
using Content.Server.Ghost;
using Content.Server.Maps;
using Content.Server.PDA;
using Content.Server.Preferences.Managers;
using Content.Server.Station;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.GameTicking;
using Robust.Server;
using Robust.Server.Maps;
using Robust.Server.ServerStatus;
using Robust.Shared.Configuration;
#if EXCEPTION_TOLERANCE
using Robust.Shared.Exceptions;
#endif
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameTicking
{
    public partial class GameTicker : SharedGameTicker
    {
        [ViewVariables] private bool _initialized;
        [ViewVariables] private bool _postInitialized;

        [ViewVariables] public MapId DefaultMap { get; private set; }

        public override void Initialize()
        {
            base.Initialize();

            DebugTools.Assert(!_initialized);
            DebugTools.Assert(!_postInitialized);

            // Initialize the other parts of the game ticker.
            InitializeStatusShell();
            InitializeCVars();
            InitializePlayer();
            InitializeLobbyMusic();
            InitializeGamePreset();
            InitializeJobController();
            InitializeUpdates();

            _initialized = true;
        }

        public void PostInitialize()
        {
            DebugTools.Assert(_initialized);
            DebugTools.Assert(!_postInitialized);

            // We restart the round now that entities are initialized and prototypes have been loaded.
            RestartRound();

            _postInitialized = true;
        }

        private void SendServerMessage(string message)
        {
            var msg = _netManager.CreateNetMessage<MsgChatMessage>();
            msg.Channel = ChatChannel.Server;
            msg.Message = message;
            IoCManager.Resolve<IServerNetManager>().ServerSendToAll(msg);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            UpdateRoundFlow(frameTime);
        }

        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IMapLoader _mapLoader = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IServerNetManager _netManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly IServerPreferencesManager _prefsManager = default!;
        [Dependency] private readonly IBaseServer _baseServer = default!;
        [Dependency] private readonly IWatchdogApi _watchdogApi = default!;
        [Dependency] private readonly IGameMapManager _gameMapManager = default!;
        [Dependency] private readonly IPauseManager _pauseManager = default!;
#if EXCEPTION_TOLERANCE
        [Dependency] private readonly IRuntimeLog _runtimeLog = default!;
#endif
        [Dependency] private readonly StationSystem _stationSystem = default!;
        [Dependency] private readonly AdminLogSystem _adminLogSystem = default!;
        [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearanceSystem = default!;
        [Dependency] private readonly PDASystem _pdaSystem = default!;
        [Dependency] private readonly DamageableSystem _damageable = default!;
        [Dependency] private readonly GhostSystem _ghosts = default!;
    }
}
