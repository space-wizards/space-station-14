using System;
using Content.Client.Administration.Managers;
using Content.Client.Changelog;
using Content.Client.CharacterInterface;
using Content.Client.Chat.Managers;
using Content.Client.EscapeMenu;
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
using Content.Client.Sandbox;
using Content.Client.Screenshot;
using Content.Client.Singularity;
using Content.Client.StationEvents;
using Content.Client.StationEvents.Managers;
using Content.Client.Stylesheets;
using Content.Client.Viewport;
using Content.Client.Voting;
using Content.Shared.Actions;
using Content.Shared.Administration;
using Content.Shared.Alert;
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
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Entry
{
    public class EntryPoint : GameClient
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IBaseClient _baseClient = default!;
        [Dependency] private readonly IEscapeMenuOwner _escapeMenuOwner = default!;
        [Dependency] private readonly IGameController _gameController = default!;
        [Dependency] private readonly IStateManager _stateManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public override void Init()
        {
            var factory = IoCManager.Resolve<IComponentFactory>();
            var prototypes = IoCManager.Resolve<IPrototypeManager>();

            factory.DoAutoRegistrations();

            foreach (var ignoreName in IgnoredComponents.List)
            {
                factory.RegisterIgnore(ignoreName);
            }

            factory.RegisterClass<SharedResearchConsoleComponent>();
            factory.RegisterClass<SharedLatheComponent>();
            factory.RegisterClass<SharedSpawnPointComponent>();
            factory.RegisterClass<SharedVendingMachineComponent>();
            factory.RegisterClass<SharedWiresComponent>();
            factory.RegisterClass<SharedCargoConsoleComponent>();
            factory.RegisterClass<SharedReagentDispenserComponent>();
            factory.RegisterClass<SharedChemMasterComponent>();
            factory.RegisterClass<SharedGravityGeneratorComponent>();
            factory.RegisterClass<SharedAMEControllerComponent>();

            prototypes.RegisterIgnore("accent");
            prototypes.RegisterIgnore("material");
            prototypes.RegisterIgnore("reaction"); //Chemical reactions only needed by server. Reactions checks are server-side.
            prototypes.RegisterIgnore("gasReaction");
            prototypes.RegisterIgnore("seed"); // Seeds prototypes are server-only.
            prototypes.RegisterIgnore("barSign");
            prototypes.RegisterIgnore("objective");
            prototypes.RegisterIgnore("holiday");
            prototypes.RegisterIgnore("aiFaction");
            prototypes.RegisterIgnore("gameMap");
            prototypes.RegisterIgnore("behaviorSet");
            prototypes.RegisterIgnore("advertisementsPack");
            prototypes.RegisterIgnore("metabolizerType");
            prototypes.RegisterIgnore("metabolismGroup");
            prototypes.RegisterIgnore("salvageMap");
            prototypes.RegisterIgnore("gamePreset");
            prototypes.RegisterIgnore("gameRule");

            ClientContentIoC.Register();

            foreach (var callback in TestingCallbacks)
            {
                var cast = (ClientModuleTestingCallbacks) callback;
                cast.ClientBeforeIoC?.Invoke();
            }

            IoCManager.BuildGraph();
            factory.GenerateNetIds();

            IoCManager.Resolve<IClientAdminManager>().Initialize();
            IoCManager.Resolve<IParallaxManager>().LoadParallax();
            IoCManager.Resolve<IBaseClient>().PlayerJoinedServer += SubscribePlayerAttachmentEvents;
            IoCManager.Resolve<IStylesheetManager>().Initialize();
            IoCManager.Resolve<IScreenshotHook>().Initialize();
            IoCManager.Resolve<ChangelogManager>().Initialize();
            IoCManager.Resolve<RulesManager>().Initialize();
            IoCManager.Resolve<ViewportManager>().Initialize();

            IoCManager.InjectDependencies(this);

            _escapeMenuOwner.Initialize();

            _baseClient.PlayerJoinedServer += (_, _) =>
            {
                IoCManager.Resolve<IMapManager>().CreateNewMapEntity(MapId.Nullspace);
            };
        }

        /// <summary>
        /// Subscribe events to the player manager after the player manager is set up
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void SubscribePlayerAttachmentEvents(object? sender, EventArgs args)
        {
            if (_playerManager.LocalPlayer != null)
            {
                _playerManager.LocalPlayer.EntityAttached += AttachPlayerToEntity;
                _playerManager.LocalPlayer.EntityDetached += DetachPlayerFromEntity;
            }
        }

        /// <summary>
        /// Add the character interface master which combines all character interfaces into one window
        /// </summary>
        public void AttachPlayerToEntity(EntityAttachedEventArgs eventArgs)
        {
            // TODO This is shitcode. Move this to an entity system, FOR FUCK'S SAKE
            _entityManager.AddComponent<CharacterInterfaceComponent>(eventArgs.NewEntity);
        }

        /// <summary>
        /// Remove the character interface master from this entity now that we have detached ourselves from it
        /// </summary>
        public void DetachPlayerFromEntity(EntityDetachedEventArgs eventArgs)
        {
            // TODO This is shitcode. Move this to an entity system, FOR FUCK'S SAKE
            if (!_entityManager.Deleted(eventArgs.OldEntity))
            {
                _entityManager.RemoveComponent<CharacterInterfaceComponent>(eventArgs.OldEntity);
            }
        }

        public override void PostInit()
        {
            base.PostInit();

            // Setup key contexts
            var inputMan = IoCManager.Resolve<IInputManager>();
            ContentContexts.SetupContexts(inputMan.Contexts);

            IoCManager.Resolve<IGameHud>().Initialize();

            var overlayMgr = IoCManager.Resolve<IOverlayManager>();
            overlayMgr.AddOverlay(new ParallaxOverlay());
            overlayMgr.AddOverlay(new SingularityOverlay());
            overlayMgr.AddOverlay(new CritOverlay()); //Hopefully we can cut down on this list... don't see why a death overlay needs to be instantiated here.
            overlayMgr.AddOverlay(new CircleMaskOverlay());
            overlayMgr.AddOverlay(new FlashOverlay());
            overlayMgr.AddOverlay(new RadiationPulseOverlay());

            IoCManager.Resolve<IChatManager>().Initialize();
            IoCManager.Resolve<ISandboxManager>().Initialize();
            IoCManager.Resolve<IClientPreferencesManager>().Initialize();
            IoCManager.Resolve<IStationEventManager>().Initialize();
            IoCManager.Resolve<EuiManager>().Initialize();
            IoCManager.Resolve<ActionManager>().Initialize();
            IoCManager.Resolve<IVoteManager>().Initialize();
            IoCManager.Resolve<IGamePrototypeLoadManager>().Initialize();

            _baseClient.RunLevelChanged += (_, args) =>
            {
                if (args.NewLevel == ClientRunLevel.Initialize)
                {
                    SwitchToDefaultState(args.OldLevel == ClientRunLevel.Connected ||
                                         args.OldLevel == ClientRunLevel.InGame);
                }
            };

            // Disable engine-default viewport since we use our own custom viewport control.
            IoCManager.Resolve<IUserInterfaceManager>().MainViewport.Visible = false;

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
                    IoCManager.Resolve<IChatManager>().FrameUpdate(frameEventArgs);
                    break;
            }
        }
    }
}
