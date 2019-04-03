using Content.Server.GameObjects;
using Content.Server.GameObjects.Components;
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
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Mobs;
using Content.Server.Players;
using Content.Server.GameObjects.Components.Interactable;
using Content.Server.GameObjects.Components.Markers;
using Content.Server.GameObjects.Components.Sound;
using Content.Server.GameObjects.Components.Weapon.Ranged;
using Content.Server.GameTicking;
using Content.Server.Interfaces;
using Content.Server.Interfaces.GameTicking;
using Content.Shared.GameObjects.Components.Inventory;
using Content.Shared.GameObjects.Components.Markers;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Interfaces;
using SS14.Server.Interfaces.ServerStatus;
using SS14.Shared.Timing;
using Content.Server.GameObjects.Components.Destructible;

namespace Content.Server
{
    public class EntryPoint : GameServer
    {
        private IGameTicker _gameTicker;
        private StatusShell _statusShell;

        /// <inheritdoc />
        public override void Init()
        {
            base.Init();

            var factory = IoCManager.Resolve<IComponentFactory>();

            factory.Register<HandsComponent>();
            factory.RegisterReference<HandsComponent, IHandsComponent>();

            factory.Register<InventoryComponent>();

            factory.Register<StoreableComponent>();
            factory.Register<ItemComponent>();
            factory.RegisterReference<ItemComponent, StoreableComponent>();
            factory.Register<ClothingComponent>();
            factory.RegisterReference<ClothingComponent, ItemComponent>();
            factory.RegisterReference<ClothingComponent, StoreableComponent>();

            factory.Register<DamageableComponent>();
            factory.Register<DestructibleComponent>();
            factory.Register<TemperatureComponent>();
            factory.Register<ServerDoorComponent>();

            //Power Components
            factory.Register<PowerTransferComponent>();
            factory.Register<PowerProviderComponent>();
            factory.RegisterReference<PowerProviderComponent, PowerDeviceComponent>();
            factory.Register<PowerNodeComponent>();
            factory.Register<PowerStorageNetComponent>();
            factory.RegisterReference<PowerStorageNetComponent, PowerStorageComponent>();
            factory.Register<PowerCellComponent>();
            factory.RegisterReference<PowerCellComponent, PowerStorageComponent>();
            factory.Register<PowerDeviceComponent>();
            factory.Register<PowerGeneratorComponent>();
            factory.Register<LightBulbComponent>();

            //Tools
            factory.Register<MultitoolComponent>();
            factory.Register<WirecutterComponent>();
            factory.Register<WrenchComponent>();
            factory.Register<WelderComponent>();
            factory.Register<ScrewdriverComponent>();
            factory.Register<CrowbarComponent>();

            factory.Register<HitscanWeaponComponent>();
            factory.Register<RangedWeaponComponent>();
            factory.Register<BallisticMagazineWeaponComponent>();
            factory.Register<ProjectileComponent>();
            factory.Register<ThrownItemComponent>();
            factory.Register<MeleeWeaponComponent>();

            factory.Register<HealingComponent>();
            factory.Register<SoundComponent>();

            factory.Register<HandheldLightComponent>();

            factory.Register<ServerStorageComponent>();
            factory.RegisterReference<ServerStorageComponent, IActivate>();

            factory.Register<PowerDebugTool>();
            factory.Register<PoweredLightComponent>();
            factory.Register<SmesComponent>();
            factory.Register<ApcComponent>();
            factory.Register<MaterialComponent>();
            factory.Register<StackComponent>();

            factory.Register<ConstructionComponent>();
            factory.Register<ConstructorComponent>();
            factory.RegisterIgnore("ConstructionGhost");

            factory.Register<MindComponent>();
            factory.Register<SpeciesComponent>();

            factory.Register<SpawnPointComponent>();
            factory.RegisterReference<SpawnPointComponent, SharedSpawnPointComponent>();

            factory.Register<BallisticBulletComponent>();
            factory.Register<BallisticMagazineComponent>();

            factory.Register<HitscanWeaponCapacitorComponent>();

            factory.Register<CameraRecoilComponent>();
            factory.RegisterReference<CameraRecoilComponent, SharedCameraRecoilComponent>();

            factory.RegisterIgnore("IconSmooth");
            factory.RegisterIgnore("SubFloorHide");

            IoCManager.Register<ISharedNotifyManager, ServerNotifyManager>();
            IoCManager.Register<IServerNotifyManager, ServerNotifyManager>();
            IoCManager.Register<IGameTicker, GameTicker>();
            IoCManager.BuildGraph();

            _gameTicker = IoCManager.Resolve<IGameTicker>();

            IoCManager.Resolve<IServerNotifyManager>().Initialize();

            var playerManager = IoCManager.Resolve<IPlayerManager>();

            _statusShell = new StatusShell();
        }

        public override void PostInit()
        {
            base.PostInit();

            _gameTicker.Initialize();
        }

        public override void Update(AssemblyLoader.UpdateLevel level, float frameTime)
        {
            base.Update(level, frameTime);

            _gameTicker.Update(new FrameEventArgs(frameTime));
        }
    }
}
