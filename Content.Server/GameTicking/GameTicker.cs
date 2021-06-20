using Content.Server.Chat.Managers;
using Content.Server.Preferences.Managers;
using Content.Shared.Chat;
using Content.Shared.GameTicking;
using Content.Shared.GameWindow;
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

            // Initialize the other parts of the game ticker.
            InitializeStatusShell();
            InitializeCVars();
            InitializePlayer();
            InitializeLobbyMusic();
            InitializeGamePreset();
            InitializeJobController();
            InitializeUpdates();

            _initialized = true;
                    Loc.GetString("game-ticker-restart-round-server-update"));
            SendServerMessage(Loc.GetString("game-ticker-restart-round"));
            SendServerMessage("game-ticker-start-round");
        }

        public void PostInitialize()
                    _chatManager.DispatchServerAnnouncement(Loc.GetString("game-ticker-start-round-cannot-start-game-mode-fallback",
                                                                         ("failedGameMode", Preset.ModeTitle),
                                                                         ("fallbackMode", newPreset.ModeTitle)));
        {
            DebugTools.Assert(_initialized);
            DebugTools.Assert(!_postInitialized);

            // We restart the round now that entities are initialized and prototypes have been loaded.
                    SendServerMessage(Loc.GetString("game-ticker-start-round-cannot-start-game-mode-restart",("failedGameMode", Preset.ModeTitle)));
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
                            : mind.AllRoles.FirstOrDefault()?.Name ?? Loc.GetString("game-ticker-unknown-role"),
            _chatManager.DispatchServerAnnouncement(Loc.GetString("game-ticker-delay-start",("seconds",time.TotalSeconds)));
            _chatManager.DispatchServerAnnouncement(Loc.GetString(Paused
                ? "game-ticker-pause-start"
                : "game-ticker-pause-start-resumed"));
                    Loc.GetString("game-ticker-restart-round-server-update"));
            _chatManager.DispatchServerMessage(session, Loc.GetString("game-ticker-player-join-game-message"));
            return Loc.GetString("game-ticker-get-info-text",("gmTitle", gmTitle),("desc", desc));
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
