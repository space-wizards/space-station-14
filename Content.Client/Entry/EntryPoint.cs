using Content.Client.Administration.Managers;
using Content.Client.Changelog;
using Content.Client.Chat.Managers;
using Content.Client.DebugMon;
using Content.Client.Eui;
using Content.Client.Fullscreen;
using Content.Client.GameTicking.Managers;
using Content.Client.GhostKick;
using Content.Client.Guidebook;
using Content.Client.Input;
using Content.Client.IoC;
using Content.Client.Launcher;
using Content.Client.Lobby;
using Content.Client.MainMenu;
using Content.Client.Parallax.Managers;
using Content.Client.Players.PlayTimeTracking;
using Content.Client.Playtime;
using Content.Client.Radiation.Overlays;
using Content.Client.Replay;
using Content.Client.Screenshot;
using Content.Client.Singularity;
using Content.Client.Stylesheets;
using Content.Client.UserInterface;
using Content.Client.Viewport;
using Content.Client.Voting;
using Content.Shared.Ame.Components;
using Content.Shared.Gravity;
using Content.Shared.Localizations;
using Robust.Client;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Replays.Loading;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using Robust.Shared.Replays;
using Robust.Shared.Timing;

namespace Content.Client.Entry
{
    public sealed class EntryPoint : GameClient
    {
        [Dependency] private readonly IBaseClient _baseClient = default!;
        [Dependency] private readonly IGameController _gameController = default!;
        [Dependency] private readonly IStateManager _stateManager = default!;
        [Dependency] private readonly IComponentFactory _componentFactory = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IClientAdminManager _adminManager = default!;
        [Dependency] private readonly IParallaxManager _parallaxManager = default!;
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly IStylesheetManager _stylesheetManager = default!;
        [Dependency] private readonly IScreenshotHook _screenshotHook = default!;
        [Dependency] private readonly FullscreenHook _fullscreenHook = default!;
        [Dependency] private readonly ChangelogManager _changelogManager = default!;
        [Dependency] private readonly ViewportManager _viewportManager = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IClientPreferencesManager _clientPreferencesManager = default!;
        [Dependency] private readonly EuiManager _euiManager = default!;
        [Dependency] private readonly IVoteManager _voteManager = default!;
        [Dependency] private readonly DocumentParsingManager _documentParsingManager = default!;
        [Dependency] private readonly GhostKickManager _ghostKick = default!;
        [Dependency] private readonly ExtendedDisconnectInformationManager _extendedDisconnectInformation = default!;
        [Dependency] private readonly JobRequirementsManager _jobRequirements = default!;
        [Dependency] private readonly ContentLocalizationManager _contentLoc = default!;
        [Dependency] private readonly ContentReplayPlaybackManager _playbackMan = default!;
        [Dependency] private readonly IResourceManager _resourceManager = default!;
        [Dependency] private readonly IReplayLoadManager _replayLoad = default!;
        [Dependency] private readonly ILogManager _logManager = default!;
        [Dependency] private readonly DebugMonitorManager _debugMonitorManager = default!;
        [Dependency] private readonly TitleWindowManager _titleWindowManager = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly ClientsidePlaytimeTrackingManager _clientsidePlaytimeManager = default!;

        public override void Init()
        {
            ClientContentIoC.Register();

            foreach (var callback in TestingCallbacks)
            {
                var cast = (ClientModuleTestingCallbacks) callback;
                cast.ClientBeforeIoC?.Invoke();
            }

            IoCManager.BuildGraph();
            IoCManager.InjectDependencies(this);

            _contentLoc.Initialize();
            _componentFactory.DoAutoRegistrations();
            _componentFactory.IgnoreMissingComponents();

            // Do not add to these, they are legacy.
            _componentFactory.RegisterClass<SharedGravityGeneratorComponent>();
            _componentFactory.RegisterClass<SharedAmeControllerComponent>();
            // Do not add to the above, they are legacy

            _prototypeManager.RegisterIgnore("utilityQuery");
            _prototypeManager.RegisterIgnore("utilityCurvePreset");
            _prototypeManager.RegisterIgnore("accent");
            _prototypeManager.RegisterIgnore("gasReaction");
            _prototypeManager.RegisterIgnore("seed"); // Seeds prototypes are server-only.
            _prototypeManager.RegisterIgnore("objective");
            _prototypeManager.RegisterIgnore("holiday");
            _prototypeManager.RegisterIgnore("htnCompound");
            _prototypeManager.RegisterIgnore("htnPrimitive");
            _prototypeManager.RegisterIgnore("gameMap");
            _prototypeManager.RegisterIgnore("gameMapPool");
            _prototypeManager.RegisterIgnore("lobbyBackground");
            _prototypeManager.RegisterIgnore("gamePreset");
            _prototypeManager.RegisterIgnore("noiseChannel");
            _prototypeManager.RegisterIgnore("playerConnectionWhitelist");
            _prototypeManager.RegisterIgnore("spaceBiome");
            _prototypeManager.RegisterIgnore("worldgenConfig");
            _prototypeManager.RegisterIgnore("gameRule");
            _prototypeManager.RegisterIgnore("worldSpell");
            _prototypeManager.RegisterIgnore("entitySpell");
            _prototypeManager.RegisterIgnore("instantSpell");
            _prototypeManager.RegisterIgnore("roundAnnouncement");
            _prototypeManager.RegisterIgnore("wireLayout");
            _prototypeManager.RegisterIgnore("alertLevels");
            _prototypeManager.RegisterIgnore("nukeopsRole");
            _prototypeManager.RegisterIgnore("ghostRoleRaffleDecider");
            _prototypeManager.RegisterIgnore("codewordGenerator");
            _prototypeManager.RegisterIgnore("codewordFaction");

            _componentFactory.GenerateNetIds();
            _adminManager.Initialize();
            _screenshotHook.Initialize();
            _fullscreenHook.Initialize();
            _changelogManager.Initialize();
            _viewportManager.Initialize();
            _ghostKick.Initialize();
            _extendedDisconnectInformation.Initialize();
            _jobRequirements.Initialize();
            _playbackMan.Initialize();
            _clientsidePlaytimeManager.Initialize();

            //AUTOSCALING default Setup!
            _configManager.SetCVar("interface.resolutionAutoScaleUpperCutoffX", 1080);
            _configManager.SetCVar("interface.resolutionAutoScaleUpperCutoffY", 720);
            _configManager.SetCVar("interface.resolutionAutoScaleLowerCutoffX", 520);
            _configManager.SetCVar("interface.resolutionAutoScaleLowerCutoffY", 240);
            _configManager.SetCVar("interface.resolutionAutoScaleMinimum", 0.5f);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _titleWindowManager.Shutdown();
        }

        public override void PostInit()
        {
            base.PostInit();

            _stylesheetManager.Initialize();

            // Setup key contexts
            ContentContexts.SetupContexts(_inputManager.Contexts);

            _parallaxManager.LoadDefaultParallax();

            _overlayManager.AddOverlay(new SingularityOverlay());
            _overlayManager.AddOverlay(new RadiationPulseOverlay());
            _chatManager.Initialize();
            _clientPreferencesManager.Initialize();
            _euiManager.Initialize();
            _voteManager.Initialize();
            _userInterfaceManager.SetDefaultTheme("SS14DefaultTheme");
            _userInterfaceManager.SetActiveTheme(_configManager.GetCVar(CVars.InterfaceTheme));
            _documentParsingManager.Initialize();
            _titleWindowManager.Initialize();

            _baseClient.RunLevelChanged += (_, args) =>
            {
                if (args.NewLevel == ClientRunLevel.Initialize)
                {
                    SwitchToDefaultState(args.OldLevel == ClientRunLevel.Connected ||
                                         args.OldLevel == ClientRunLevel.InGame);
                }
            };

            // Disable engine-default viewport since we use our own custom viewport control.
            _userInterfaceManager.MainViewport.Visible = false;

            SwitchToDefaultState();
        }

        private void SwitchToDefaultState(bool disconnected = false)
        {
            // Fire off into state dependent on launcher or not.

            // Check if we're loading a replay via content bundle!
            if (_configManager.GetCVar(CVars.LaunchContentBundle)
                && _resourceManager.ContentFileExists(
                    ReplayConstants.ReplayZipFolder.ToRootedPath() / ReplayConstants.FileMeta))
            {
                _logManager.GetSawmill("entry").Info("Loading content bundle replay from VFS!");

                var reader = new ReplayFileReaderResources(
                    _resourceManager,
                    ReplayConstants.ReplayZipFolder.ToRootedPath());

                _playbackMan.LastLoad = (null, ReplayConstants.ReplayZipFolder.ToRootedPath());
                _replayLoad.LoadAndStartReplay(reader);
            }
            else if (_gameController.LaunchState.FromLauncher)
            {
                _stateManager.RequestStateChange<LauncherConnecting>();
                var state = (LauncherConnecting) _stateManager.CurrentState;

                if (disconnected)
                {
                    state.SetDisconnected();
                }
            }
            else
            {
                _stateManager.RequestStateChange<MainScreen>();
            }
        }

        public override void Update(ModUpdateLevel level, FrameEventArgs frameEventArgs)
        {
            if (level == ModUpdateLevel.FramePreEngine)
            {
                _debugMonitorManager.FrameUpdate();
            }

            if (level == ModUpdateLevel.PreEngine)
            {
                if (_baseClient.RunLevel is ClientRunLevel.InGame or ClientRunLevel.SinglePlayerGame)
                {
                    var updateSystem = _entitySystemManager.GetEntitySystem<BuiPreTickUpdateSystem>();
                    updateSystem.RunUpdates();
                }
            }
        }
    }
}
