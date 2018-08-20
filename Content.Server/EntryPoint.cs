using Content.Server.GameObjects;
using Content.Server.GameObjects.Components.Power;
using Content.Server.GameObjects.Components.Interactable.Tools;
using Content.Server.Interfaces.GameObjects;
using Content.Server.Placement;
using SS14.Server;
using SS14.Server.Interfaces;
using SS14.Server.Interfaces.Chat;
using SS14.Server.Interfaces.Maps;
using SS14.Server.Interfaces.Player;
using SS14.Server.Player;
using SS14.Shared.Console;
using SS14.Shared.ContentPack;
using SS14.Shared.Enums;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Interfaces.Timers;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Map;
using SS14.Shared.Timers;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.Maths;
using Content.Server.GameObjects.Components.Weapon.Ranged.Hitscan;
using Content.Server.GameObjects.Components.Weapon.Ranged.Projectile;
using Content.Server.GameObjects.Components.Projectiles;
using Content.Server.GameObjects.Components.Weapon.Melee;
using Content.Server.GameObjects.Components.Materials;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.GameObjects.Components.Construction;

namespace Content.Server
{
    public class EntryPoint : GameServer
    {
        private IBaseServer _server;
        private IPlayerManager _players;
        private IEntityManager entityManager;
        private IChatManager chatManager;

        private bool _countdownStarted;

        /// <inheritdoc />
        public override void Init()
        {
            base.Init();

            _server = IoCManager.Resolve<IBaseServer>();
            _players = IoCManager.Resolve<IPlayerManager>();
            entityManager = IoCManager.Resolve<IEntityManager>();
            chatManager = IoCManager.Resolve<IChatManager>();

            _server.RunLevelChanged += HandleRunLevelChanged;
            _players.PlayerStatusChanged += HandlePlayerStatusChanged;
            _players.PlayerPrototypeName = "HumanMob_Content";

            var factory = IoCManager.Resolve<IComponentFactory>();

            factory.Register<HandsComponent>();
            factory.RegisterReference<HandsComponent, IHandsComponent>();

            factory.Register<InventoryComponent>();

            factory.Register<StoreableComponent>();
            factory.Register<ItemComponent>();
            factory.RegisterReference<ItemComponent, StoreableComponent>();
            factory.Register<ClothingComponent>();
            factory.RegisterReference<ClothingComponent, ItemComponent>();

            factory.Register<DamageableComponent>();
            factory.Register<DestructibleComponent>();
            factory.Register<TemperatureComponent>();
            factory.Register<ServerDoorComponent>();

            //Power Components
            factory.Register<PowerTransferComponent>();
            factory.Register<PowerProviderComponent>();
            factory.RegisterReference<PowerProviderComponent, PowerDeviceComponent>();
            factory.Register<PowerNodeComponent>();
            factory.Register<PowerStorageComponent>();
            factory.Register<PowerDeviceComponent>();
            factory.Register<PowerGeneratorComponent>();

            //Tools
            factory.Register<MultitoolComponent>();
            factory.Register<WirecutterComponent>();
            factory.Register<WrenchComponent>();
            factory.Register<WelderComponent>();
            factory.Register<ScrewdriverComponent>();
            factory.Register<CrowbarComponent>();

            factory.Register<HitscanWeaponComponent>();
            factory.Register<ProjectileWeaponComponent>();
            factory.Register<ProjectileComponent>();
            factory.Register<MeleeWeaponComponent>();

            factory.Register<ServerStorageComponent>();

            factory.Register<PowerDebugTool>();
            factory.Register<PoweredLightComponent>();
            factory.Register<SmesComponent>();
            factory.Register<ApcComponent>();
            factory.Register<MaterialComponent>();
            factory.Register<StackComponent>();

            factory.Register<ConstructionComponent>();
            factory.Register<ConstructorComponent>();
            factory.RegisterIgnore("ConstructionGhost");
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _server.RunLevelChanged -= HandleRunLevelChanged;
                _players.PlayerStatusChanged -= HandlePlayerStatusChanged;
            }

            base.Dispose(disposing);
        }

        private void HandleRunLevelChanged(object sender, RunLevelChangedEventArgs args)
        {
            switch (args.NewLevel)
            {
                case ServerRunLevel.PreGame:
                    var timing = IoCManager.Resolve<IGameTiming>();
                    var mapLoader = IoCManager.Resolve<IMapLoader>();
                    var mapMan = IoCManager.Resolve<IMapManager>();

                    var newMap = mapMan.CreateMap();
                    var grid = mapLoader.LoadBlueprint(newMap, "Maps/stationstation.yml");

                    _players.FallbackSpawnPoint = new GridLocalCoordinates(Vector2.Zero, grid);

                    var startTime = timing.RealTime;
                    var timeSpan = timing.RealTime - startTime;
                    Logger.Info($"Loaded map in {timeSpan.TotalMilliseconds:N2}ms.");

                    chatManager.DispatchMessage(ChatChannel.Server, "Gamemode: Round loaded!");
                    break;
                case ServerRunLevel.Game:
                    _players.SendJoinGameToAll();
                    chatManager.DispatchMessage(ChatChannel.Server, "Gamemode: Round started!");
                    break;
                case ServerRunLevel.PostGame:
                    chatManager.DispatchMessage(ChatChannel.Server, "Gamemode: Round over!");
                    break;
            }
        }

        private void HandlePlayerStatusChanged(object sender, SessionStatusEventArgs args)
        {
            switch (args.NewStatus)
            {
                case SessionStatus.Connected:
                    {
                        // timer time must be > tick length
                        Timer.Spawn(250, args.Session.JoinLobby);

                        chatManager.DispatchMessage(ChatChannel.Server, "Gamemode: Player joined server!", args.Session.SessionId);
                    }
                    break;

                case SessionStatus.InLobby:
                    {
                        // auto start game when first player joins
                        if (_server.RunLevel == ServerRunLevel.PreGame && !_countdownStarted)
                        {
                            _countdownStarted = true;
                            Timer.Spawn(2000, () =>
                            {
                                _server.RunLevel = ServerRunLevel.Game;
                                _countdownStarted = false;
                            });
                        }

                        chatManager.DispatchMessage(ChatChannel.Server, "Gamemode: Player joined Lobby!", args.Session.SessionId);
                    }
                    break;

                case SessionStatus.InGame:
                    {
                        //TODO: Check for existing mob and re-attach
                        var session = args.Session;
                        if (session.Data.AttachedEntityUid.HasValue
                        && entityManager.TryGetEntity(session.Data.AttachedEntityUid.Value, out var entity))
                        {
                            session.AttachToEntity(entity);
                        }
                        else
                        {
                            _players.SpawnPlayerMob(args.Session);
                        }
                        chatManager.DispatchMessage(ChatChannel.Server, "Gamemode: Player joined Game!", args.Session.SessionId);
                    }
                    break;

                case SessionStatus.Disconnected:
                    {
                        chatManager.DispatchMessage(ChatChannel.Server, "Gamemode: Player left!", args.Session.SessionId);
                    }
                    break;
            }
        }
    }
}
