using System;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules;
using Content.Server.Preferences.Managers;
using Content.Shared.Chat;
using Content.Shared.GameTicking;
using Content.Shared.GameWindow;
using Prometheus;
using Robust.Server;
using Robust.Server.Maps;
using Robust.Server.ServerStatus;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Reflection;
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
        [ViewVariables] public GridId DefaultGridId { get; private set; }

        public override void Initialize()
        {
            base.Initialize();

            DebugTools.Assert(!_initialized);
            DebugTools.Assert(!_postInitialized);

            _netManager.RegisterNetMessage<MsgTickerJoinLobby>(nameof(MsgTickerJoinLobby));
            _netManager.RegisterNetMessage<MsgTickerJoinGame>(nameof(MsgTickerJoinGame));
            _netManager.RegisterNetMessage<MsgTickerLobbyStatus>(nameof(MsgTickerLobbyStatus));
            _netManager.RegisterNetMessage<MsgTickerLobbyInfo>(nameof(MsgTickerLobbyInfo));
            _netManager.RegisterNetMessage<MsgTickerLobbyCountdown>(nameof(MsgTickerLobbyCountdown));
            _netManager.RegisterNetMessage<MsgTickerLobbyReady>(nameof(MsgTickerLobbyReady));
            _netManager.RegisterNetMessage<MsgRoundEndMessage>(nameof(MsgRoundEndMessage));
            _netManager.RegisterNetMessage<MsgRequestWindowAttention>(nameof(MsgRequestWindowAttention));
            _netManager.RegisterNetMessage<MsgTickerLateJoinStatus>(nameof(MsgTickerLateJoinStatus));
            _netManager.RegisterNetMessage<MsgTickerJobsAvailable>(nameof(MsgTickerJobsAvailable));

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

        public void ToggleDisallowLateJoin(bool disallowLateJoin)
        {
            DisallowLateJoin = disallowLateJoin;
            UpdateLateJoinStatus();
            UpdateJobsAvailable();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            UpdateRoundFlow(frameTime);
        }

        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IMapLoader _mapLoader = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IServerNetManager _netManager = default!;
        [Dependency] private readonly IDynamicTypeFactory _dynamicTypeFactory = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly IServerPreferencesManager _prefsManager = default!;
        [Dependency] private readonly IBaseServer _baseServer = default!;
        [Dependency] private readonly IWatchdogApi _watchdogApi = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;
    }
}
