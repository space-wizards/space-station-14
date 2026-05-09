using System.Threading.Tasks;
using Content.Server.Acz;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Afk;
using Content.Server.Chat.Managers;
using Content.Server.Connection;
using Content.Server.Database;
using Content.Server.Discord.DiscordLink;
using Content.Server.EUI;
using Content.Server.FeedbackSystem;
using Content.Server.GameTicking;
using Content.Server.GhostKick;
using Content.Server.GuideGenerator;
using Content.Server.Info;
using Content.Server.IoC;
using Content.Server.Maps;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.Players.JobWhitelist;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Players.RateLimiting;
using Content.Server.Preferences.Managers;
using Content.Server.ServerInfo;
using Content.Server.ServerUpdates;
using Content.Server.Voting.Managers;
using Content.Shared.CCVar;
using Content.Shared.FeedbackSystem;
using Content.Shared.Kitchen;
using Content.Shared.Localizations;
using Robust.Server;
using Robust.Server.ServerStatus;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Entry
{
    public sealed partial class EntryPoint : GameServer
    {
        internal const string ConfigPresetsDir = "/ConfigPresets/";
        private const string ConfigPresetsDirBuild = $"{ConfigPresetsDir}Build/";

        [Dependency] private CVarControlManager _cvarCtrl = default!;
        [Dependency] private ContentLocalizationManager _loc = default!;
        [Dependency] private ContentNetworkResourceManager _netResMan = default!;
        [Dependency] private DiscordChatLink _discordChatLink = default!;
        [Dependency] private DiscordLink _discordLink = default!;
        [Dependency] private EuiManager _euiManager = default!;
        [Dependency] private GhostKickManager _ghostKick = default!;
        [Dependency] private IAdminManager _admin = default!;
        [Dependency] private IAdminLogManager _adminLog = default!;
        [Dependency] private IAfkManager _afk = default!;
        [Dependency] private IBanManager _ban = default!;
        [Dependency] private IChatManager _chatSan = default!;
        [Dependency] private IChatSanitizationManager _chat = default!;
        [Dependency] private IComponentFactory _factory = default!;
        [Dependency] private IConfigurationManager _cfg = default!;
        [Dependency] private IConnectionManager _connection = default!;
        [Dependency] private IEntitySystemManager _entSys = default!;
        [Dependency] private IGameMapManager _gameMap = default!;
        [Dependency] private ILogManager _log = default!;
        [Dependency] private INodeGroupFactory _nodeFactory = default!;
        [Dependency] private IPrototypeManager _proto = default!;
        [Dependency] private IResourceManager _res = default!;
        [Dependency] private IServerDbManager _dbManager = default!;
        [Dependency] private IServerPreferencesManager _preferences = default!;
        [Dependency] private IStatusHost _host = default!;
        [Dependency] private IVoteManager _voteManager = default!;
        [Dependency] private IWatchlistWebhookManager _watchlistWebhookManager = default!;
        [Dependency] private JobWhitelistManager _job = default!;
        [Dependency] private MultiServerKickManager _multiServerKick = default!;
        [Dependency] private PlayTimeTrackingManager _playTimeTracking = default!;
        [Dependency] private PlayerRateLimitManager _rateLimit = default!;
        [Dependency] private RecipeManager _recipe = default!;
        [Dependency] private RulesManager _rules = default!;
        [Dependency] private ServerApi _serverApi = default!;
        [Dependency] private ServerInfoManager _serverInfo = default!;
        [Dependency] private ServerUpdateManager _updateManager = default!;
        [Dependency] private ServerFeedbackManager _feedbackManager = null!;

        public override void PreInit()
        {
            ServerContentIoC.Register(Dependencies);
            foreach (var callback in TestingCallbacks)
            {
                var cast = (ServerModuleTestingCallbacks)callback;
                cast.ServerBeforeIoC?.Invoke();
            }

            Dependencies.Resolve<IRobustSerializer>().FloatFlags = SerializerFloatFlags.RemoveReadNan;
        }

        /// <inheritdoc />
        public override void Init()
        {
            base.Init();
            Dependencies.BuildGraph();
            Dependencies.InjectDependencies(this);

            LoadConfigPresets(_cfg, _res, _log.GetSawmill("configpreset"));

            var aczProvider = new ContentMagicAczProvider(Dependencies);
            _host.SetMagicAczProvider(aczProvider);

            _factory.DoAutoRegistrations();
            _factory.IgnoreMissingComponents("Visuals");
            _factory.RegisterIgnore(IgnoredComponents.List);
            _factory.GenerateNetIds();

            _proto.RegisterIgnore("parallax");

            _loc.Initialize();

            var dest = _cfg.GetCVar(CCVars.DestinationFile);
            if (!string.IsNullOrEmpty(dest))
                return; //hacky but it keeps load times for the generator down.

            _log.GetSawmill("Storage").Level = LogLevel.Info;
            _log.GetSawmill("db.ef").Level = LogLevel.Info;

            _adminLog.Initialize();
            _connection.Initialize();
            _dbManager.Init();
            _preferences.Init();
            _nodeFactory.Initialize();
            _netResMan.Initialize();
            _ghostKick.Initialize();
            _serverInfo.Initialize();
            _serverApi.Initialize();
            _voteManager.Initialize();
            _updateManager.Initialize();
            _playTimeTracking.Initialize();
            _watchlistWebhookManager.Initialize();
            _job.Initialize();
            _rateLimit.Initialize();
        }

        public override void PostInit()
        {
            base.PostInit();

            _chatSan.Initialize();
            _chat.Initialize();
            var dest = _cfg.GetCVar(CCVars.DestinationFile);
            if (!string.IsNullOrEmpty(dest))
            {
                var resPath = new ResPath(dest).ToRootedPath();
                var file = _res.UserData.OpenWriteText(resPath.WithName("chem_" + dest));
                ChemistryJsonGenerator.PublishJson(file);
                file.Flush();
                file = _res.UserData.OpenWriteText(resPath.WithName("react_" + dest));
                ReactionJsonGenerator.PublishJson(file);
                file.Flush();
                Dependencies.Resolve<IBaseServer>().Shutdown("Data generation done");
                return;
            }

            _recipe.Initialize();
            _admin.Initialize();
            _afk.Initialize();
            _rules.Initialize();
            _discordLink.Initialize();
            _discordChatLink.Initialize();
            _euiManager.Initialize();
            _gameMap.Initialize();
            _entSys.GetEntitySystem<GameTicker>().PostInitialize();
            _ban.Initialize();
            _connection.PostInit();
            _multiServerKick.Initialize();
            _cvarCtrl.Initialize();
            _feedbackManager.Initialize();
        }

        public override void Update(ModUpdateLevel level, FrameEventArgs frameEventArgs)
        {
            base.Update(level, frameEventArgs);

            switch (level)
            {
                case ModUpdateLevel.PostEngine:
                {
                    _euiManager.SendUpdates();
                    _voteManager.Update();
                    break;
                }

                case ModUpdateLevel.FramePostEngine:
                    _updateManager.Update();
                    _playTimeTracking.Update();
                    _watchlistWebhookManager.Update();
                    _connection.Update();
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            var dest = _cfg.GetCVar(CCVars.DestinationFile);
            if (!string.IsNullOrEmpty(dest))
            {
                _playTimeTracking.Shutdown();
                _dbManager.Shutdown();
            }

            _serverApi.Shutdown();

            // We don't care when or how this finishes, just spin the task off into the void.
            _ = _discordLink.Shutdown();
            _discordChatLink.Shutdown();
        }

        private static void LoadConfigPresets(IConfigurationManager cfg, IResourceManager res, ISawmill sawmill)
        {
            LoadBuildConfigPresets(cfg, res, sawmill);

            var presets = cfg.GetCVar(CCVars.ConfigPresets);
            if (presets == "")
                return;

            foreach (var preset in presets.Split(','))
            {
                var path = $"{ConfigPresetsDir}{preset}.toml";
                if (!res.TryContentFileRead(path, out var file))
                {
                    sawmill.Error("Unable to load config preset {Preset}!", path);
                    continue;
                }

                cfg.LoadDefaultsFromTomlStream(file);
                sawmill.Info("Loaded config preset: {Preset}", path);
            }
        }

        private static void LoadBuildConfigPresets(IConfigurationManager cfg, IResourceManager res, ISawmill sawmill)
        {
#if TOOLS
            Load(CCVars.ConfigPresetDevelopment, "development");
#endif
#if DEBUG
            Load(CCVars.ConfigPresetDebug, "debug");
#endif

#pragma warning disable CS8321
            void Load(CVarDef<bool> cVar, string name)
            {
                var path = $"{ConfigPresetsDirBuild}{name}.toml";
                if (cfg.GetCVar(cVar) && res.TryContentFileRead(path, out var file))
                {
                    cfg.LoadDefaultsFromTomlStream(file);
                    sawmill.Info("Loaded config preset: {Preset}", path);
                }
            }
#pragma warning restore CS8321
        }
    }
}
