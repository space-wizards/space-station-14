using Content.Server.Chat;
using Content.Server.GameObjects;
using Content.Server.GameObjects.Components;
using Content.Server.GameObjects.Components.Power;
using Content.Server.GameObjects.Components.Interactable.Tools;
using Content.Server.Interfaces.GameObjects;
using Content.Server.Placement;
using Robust.Server;
using Robust.Server.Interfaces;
using Robust.Server.Interfaces.Maps;
using Robust.Server.Interfaces.Player;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Enums;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Timers;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Timers;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.Maths;
using Content.Server.GameObjects.Components.Weapon.Ranged.Hitscan;
using Content.Server.GameObjects.Components.Weapon.Ranged.Projectile;
using Content.Server.GameObjects.Components.Projectiles;
using Content.Server.GameObjects.Components.Weapon.Melee;
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
using Content.Shared.GameObjects.Components.Materials;
using Content.Shared.GameObjects.Components.Inventory;
using Content.Shared.GameObjects.Components.Markers;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Interfaces;
using Robust.Server.Interfaces.ServerStatus;
using Robust.Shared.Timing;
using Content.Server.GameObjects.Components.Destructible;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Items.Storage.Fill;
using Content.Server.GameObjects.Components.Movement;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameObjects.Components.Movement;
using Content.Server.GameObjects.Components.Research;
using Content.Shared.GameObjects.Components.Research;
using Robust.Shared.Interfaces.Log;
using Content.Server.GameObjects.Components.Explosive;
using Content.Server.GameObjects.Components.Triggers;

namespace Content.Server
{
    public class EntryPoint : GameServer
    {
        private IGameTicker _gameTicker;
        private IMoMMILink _mommiLink;
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
            factory.Register<PlaceableSurfaceComponent>();

            factory.Register<DamageableComponent>();
            factory.Register<DestructibleComponent>();
            factory.Register<TemperatureComponent>();
            factory.Register<ServerDoorComponent>();
            factory.RegisterReference<ServerDoorComponent, IActivate>();

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
            factory.RegisterReference<ServerStorageComponent, IStorageComponent>();
            factory.RegisterReference<ServerStorageComponent, IActivate>();
            factory.Register<EntityStorageComponent>();
            factory.RegisterReference<EntityStorageComponent, IStorageComponent>();
            factory.RegisterReference<EntityStorageComponent, IActivate>();

            factory.Register<ToolLockerFillComponent>();
            factory.Register<ToolboxElectricalFillComponent>();

            factory.Register<PowerDebugTool>();
            factory.Register<PoweredLightComponent>();
            factory.Register<SmesComponent>();
            factory.Register<ApcComponent>();
            factory.RegisterReference<ApcComponent, IActivate>();
            factory.Register<MaterialComponent>();
            factory.Register<StackComponent>();
            factory.Register<MaterialStorageComponent>();
            factory.RegisterReference<MaterialStorageComponent, SharedMaterialStorageComponent>();

            factory.Register<ConstructionComponent>();
            factory.Register<ConstructorComponent>();
            factory.RegisterIgnore("ConstructionGhost");

            factory.Register<MindComponent>();
            factory.Register<SpeciesComponent>();
            factory.Register<HeatResistanceComponent>();

            factory.Register<SpawnPointComponent>();
            factory.RegisterReference<SpawnPointComponent, SharedSpawnPointComponent>();

            factory.Register<LatheComponent>();
            factory.RegisterReference<LatheComponent, IActivate>();
            factory.Register<LatheDatabaseComponent>();

            factory.RegisterReference<LatheDatabaseComponent, SharedLatheDatabaseComponent>();

            factory.Register<BallisticBulletComponent>();
            factory.Register<BallisticMagazineComponent>();

            factory.Register<HitscanWeaponCapacitorComponent>();

            factory.Register<CameraRecoilComponent>();
            factory.RegisterReference<CameraRecoilComponent, SharedCameraRecoilComponent>();

            factory.RegisterIgnore("IconSmooth");
            factory.RegisterIgnore("SubFloorHide");

            factory.Register<PlayerInputMoverComponent>();
            factory.RegisterReference<PlayerInputMoverComponent, IMoverComponent>();

            factory.Register<AiControllerComponent>();

            factory.Register<CatwalkComponent>();

            factory.Register<ExplosiveComponent>();
            factory.Register<OnUseTimerTriggerComponent>();

            factory.Register<FootstepModifierComponent>();
            factory.Register<EmitSoundOnUseComponent>();

            IoCManager.Register<ISharedNotifyManager, ServerNotifyManager>();
            IoCManager.Register<IServerNotifyManager, ServerNotifyManager>();
            IoCManager.Register<IGameTicker, GameTicker>();
            IoCManager.Register<IChatManager, ChatManager>();
            IoCManager.Register<IMoMMILink, MoMMILink>();
            IoCManager.BuildGraph();

            _gameTicker = IoCManager.Resolve<IGameTicker>();

            IoCManager.Resolve<IServerNotifyManager>().Initialize();
            IoCManager.Resolve<IChatManager>().Initialize();

            _mommiLink = IoCManager.Resolve<IMoMMILink>();

            var playerManager = IoCManager.Resolve<IPlayerManager>();

            _statusShell = new StatusShell();

            var logManager = IoCManager.Resolve<ILogManager>();
            logManager.GetSawmill("Storage").Level = LogLevel.Info;
        }

        public override void PostInit()
        {
            base.PostInit();

            _gameTicker.Initialize();
        }

        public override void Update(ModUpdateLevel level, float frameTime)
        {
            base.Update(level, frameTime);

            _gameTicker.Update(new FrameEventArgs(frameTime));
        }
    }
}
