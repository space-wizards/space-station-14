using Content.Client.Administration.Managers;
using Content.Client.Changelog;
using Content.Client.Chat.Managers;
using Content.Client.Eui;
using Content.Client.Flash;
using Content.Client.HUD;
using Content.Client.Info;
using Content.Client.Input;
using Content.Client.IoC;
using Content.Client.Launcher;
using Content.Client.MainMenu;
using Content.Client.MobState.Overlays;
using Content.Client.Parallax;
using Content.Client.Parallax.Managers;
using Content.Client.Preferences;
using Content.Client.Screenshot;
using Content.Client.Singularity;
using Content.Client.StationEvents;
using Content.Client.StationEvents.Managers;
using Content.Client.Stylesheets;
using Content.Client.Viewport;
using Content.Client.Voting;
using Content.Shared.Administration;
using Content.Shared.AME;
using Content.Shared.Cargo.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Dispenser;
using Content.Shared.Gravity;
using Content.Shared.Lathe;
using Content.Shared.Markers;
using Content.Shared.Research.Components;
using Content.Shared.VendingMachines;
using Content.Shared.Wires;
using Robust.Client;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Themes;
using Robust.Shared.ContentPack;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Entry
{
    public sealed class EntryPoint : GameClient
    {
        [Dependency] private readonly IHudManager _hudManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IBaseClient _baseClient = default!;
        [Dependency] private readonly IGameController _gameController = default!;
        [Dependency] private readonly IStateManager _stateManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IComponentFactory _componentFactory = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IClientAdminManager _adminManager = default!;
        [Dependency] private readonly IParallaxManager _parallaxManager = default!;
        [Dependency] private readonly IStylesheetManager _stylesheetManager = default!;
        [Dependency] private readonly IScreenshotHook _screenshotHook = default!;
        [Dependency] private readonly ChangelogManager _changelogManager = default!;
        [Dependency] private readonly RulesManager _rulesManager = default!;
        [Dependency] private readonly ViewportManager _viewportManager = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IClientPreferencesManager _clientPreferencesManager = default!;
        [Dependency] private readonly IStationEventManager _stationEventManager = default!;
        [Dependency] private readonly EuiManager _euiManager = default!;
        [Dependency] private readonly IVoteManager _voteManager = default!;
        [Dependency] private readonly IGamePrototypeLoadManager _gamePrototypeLoadManager = default!;
        [Dependency] private readonly IUIControllerManager _uiControllerManager = default!;
        [Dependency] private readonly NetworkResourceManager _networkResources = default!;

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

            _componentFactory.DoAutoRegistrations();

            foreach (var ignoreName in IgnoredComponents.List)
            {
                _componentFactory.RegisterIgnore(ignoreName);
            }

            _componentFactory.RegisterClass<SharedResearchConsoleComponent>();
            _componentFactory.RegisterClass<SharedLatheComponent>();
            _componentFactory.RegisterClass<SharedSpawnPointComponent>();
            _componentFactory.RegisterClass<SharedVendingMachineComponent>();
            _componentFactory.RegisterClass<SharedWiresComponent>();
            _componentFactory.RegisterClass<SharedCargoConsoleComponent>();
            _componentFactory.RegisterClass<SharedReagentDispenserComponent>();
            _componentFactory.RegisterClass<SharedChemMasterComponent>();
            _componentFactory.RegisterClass<SharedGravityGeneratorComponent>();
            _componentFactory.RegisterClass<SharedAMEControllerComponent>();

            _prototypeManager.RegisterIgnore("accent");
            _prototypeManager.RegisterIgnore("material");
            _prototypeManager.RegisterIgnore("reaction"); //Chemical reactions only needed by server. Reactions checks are server-side.
            _prototypeManager.RegisterIgnore("gasReaction");
            _prototypeManager.RegisterIgnore("seed"); // Seeds prototypes are server-only.
            _prototypeManager.RegisterIgnore("barSign");
            _prototypeManager.RegisterIgnore("objective");
            _prototypeManager.RegisterIgnore("holiday");
            _prototypeManager.RegisterIgnore("aiFaction");
            _prototypeManager.RegisterIgnore("gameMap");
            _prototypeManager.RegisterIgnore("behaviorSet");
            _prototypeManager.RegisterIgnore("lobbyBackground");
            _prototypeManager.RegisterIgnore("advertisementsPack");
            _prototypeManager.RegisterIgnore("metabolizerType");
            _prototypeManager.RegisterIgnore("metabolismGroup");
            _prototypeManager.RegisterIgnore("salvageMap");
            _prototypeManager.RegisterIgnore("gamePreset");
            _prototypeManager.RegisterIgnore("gameRule");
            _prototypeManager.RegisterIgnore("worldSpell");
            _prototypeManager.RegisterIgnore("entitySpell");
            _prototypeManager.RegisterIgnore("instantSpell");

            _componentFactory.GenerateNetIds();
            _adminManager.Initialize();
            _parallaxManager.LoadParallax();
            _stylesheetManager.Initialize();
            _screenshotHook.Initialize();
            _changelogManager.Initialize();
            _rulesManager.Initialize();
            _viewportManager.Initialize();
            _hudManager.Initialize();//TODO: this is going to break shortly
            _baseClient.PlayerJoinedServer += (_, _) => { _hudManager.Startup();}; //TODO: Move this
            _baseClient.PlayerLeaveServer += (_, _) => { _hudManager.Shutdown();};
            _baseClient.PlayerJoinedServer += (_, _) => { _mapManager.CreateNewMapEntity(MapId.Nullspace);};
        }

        public override void PostInit()
        {
            base.PostInit();
            // Setup key contexts
            ContentContexts.SetupContexts(_inputManager.Contexts);
            _overlayManager.AddOverlay(new ParallaxOverlay());
            _overlayManager.AddOverlay(new SingularityOverlay());
            _overlayManager.AddOverlay(new CritOverlay()); //Hopefully we can cut down on this list... don't see why a death overlay needs to be instantiated here.
            _overlayManager.AddOverlay(new CircleMaskOverlay());
            _overlayManager.AddOverlay(new FlashOverlay());
            _overlayManager.AddOverlay(new RadiationPulseOverlay());

            _chatManager.Initialize();
            _clientPreferencesManager.Initialize();
            _stationEventManager.Initialize();
            _euiManager.Initialize();
            _voteManager.Initialize();
            _gamePrototypeLoadManager.Initialize();
            _networkResources.Initialize();
            _userInterfaceManager.SetDefaultTheme("SS14DefaultTheme");

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

            if (_gameController.LaunchState.FromLauncher)
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
            base.Update(level, frameEventArgs);

            switch (level)
            {
                case ModUpdateLevel.FramePreEngine:
                    // TODO: Turn IChatManager into an EntitySystem and remove the line below.
                    _chatManager.FrameUpdate(frameEventArgs);
                    break;
            }
        }
    }
}
