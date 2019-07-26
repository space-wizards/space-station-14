using Content.Client.GameObjects;
using Content.Client.GameObjects.Components.Actor;
using Content.Client.GameObjects.Components.Clothing;
using Content.Client.GameObjects.Components.Construction;
using Content.Client.GameObjects.Components.Power;
using Content.Client.GameObjects.Components.IconSmoothing;
using Content.Client.GameObjects.Components.Storage;
using Content.Client.GameObjects.Components.Weapons.Ranged;
using Content.Client.GameTicking;
using Content.Client.Input;
using Content.Client.Interfaces;
using Content.Client.Interfaces.GameObjects;
using Content.Client.Interfaces.Parallax;
using Content.Client.Parallax;
using Content.Shared.Interfaces;
using Robust.Client;
using Robust.Client.Interfaces;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Client.Interfaces.Input;
using Robust.Client.Player;
using Robust.Shared.ContentPack;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using System;
using Content.Client.Chat;
using Content.Client.GameObjects.Components;
using Content.Client.GameObjects.Components.Mobs;
using Content.Client.GameObjects.Components.Movement;
using Content.Client.GameObjects.Components.Research;
using Content.Client.GameObjects.Components.Sound;
using Content.Client.Interfaces.Chat;
using Content.Client.UserInterface;
using Content.Shared.GameObjects.Components.Markers;
using Content.Shared.GameObjects.Components.Materials;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.GameObjects.Components.Research;
using Robust.Client.Interfaces.State;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.State.States;

namespace Content.Client
{
    public class EntryPoint : GameClient
    {
#pragma warning disable 649
        [Dependency] private readonly IPlayerManager _playerManager;
        [Dependency] private readonly IEscapeMenuOwner _escapeMenuOwner;
#pragma warning restore 649

        public override void Init()
        {
            var factory = IoCManager.Resolve<IComponentFactory>();
            var prototypes = IoCManager.Resolve<IPrototypeManager>();

            factory.RegisterIgnore("Interactable");
            factory.RegisterIgnore("Destructible");
            factory.RegisterIgnore("Temperature");
            factory.RegisterIgnore("PowerTransfer");
            factory.RegisterIgnore("PowerNode");
            factory.RegisterIgnore("PowerProvider");
            factory.RegisterIgnore("PowerDevice");
            factory.RegisterIgnore("PowerStorage");
            factory.RegisterIgnore("PowerGenerator");

            factory.RegisterIgnore("Explosive");
            factory.RegisterIgnore("OnUseTimerTrigger");

            factory.RegisterIgnore("ToolboxElectricalFill");
            factory.RegisterIgnore("ToolLockerFill");

            factory.RegisterIgnore("EmitSoundOnUse");
            factory.RegisterIgnore("FootstepModifier");

            factory.RegisterIgnore("HeatResistance");
            factory.RegisterIgnore("CombatMode");

            factory.RegisterIgnore("Teleportable");
            factory.RegisterIgnore("ItemTeleporter");
            factory.RegisterIgnore("Portal");

            factory.RegisterIgnore("EntityStorage");
            factory.RegisterIgnore("PlaceableSurface");

            factory.RegisterIgnore("Wirecutter");
            factory.RegisterIgnore("Screwdriver");
            factory.RegisterIgnore("Multitool");
            factory.RegisterIgnore("Welder");
            factory.RegisterIgnore("Wrench");
            factory.RegisterIgnore("Crowbar");
            factory.Register<ClientRangedWeaponComponent>();
            factory.RegisterIgnore("HitscanWeapon");
            factory.RegisterIgnore("ProjectileWeapon");
            factory.RegisterIgnore("Projectile");
            factory.RegisterIgnore("MeleeWeapon");

            factory.RegisterIgnore("Storeable");

            factory.RegisterIgnore("Stack");

            factory.RegisterIgnore("Dice");

            factory.Register<HandsComponent>();
            factory.RegisterReference<HandsComponent, IHandsComponent>();
            factory.Register<ClientStorageComponent>();
            factory.Register<ClientInventoryComponent>();
            factory.Register<PowerDebugTool>();
            factory.Register<ConstructorComponent>();
            factory.Register<ConstructionGhostComponent>();
            factory.Register<IconSmoothComponent>();
            factory.Register<LowWallComponent>();
            factory.RegisterReference<LowWallComponent, IconSmoothComponent>();
            factory.Register<DamageableComponent>();
            factory.Register<ClothingComponent>();
            factory.Register<ItemComponent>();
            factory.Register<MaterialComponent>();
            factory.Register<SoundComponent>();
            factory.Register<MaterialStorageComponent>();
            factory.RegisterReference<MaterialStorageComponent, SharedMaterialStorageComponent>();

            factory.RegisterReference<ClothingComponent, ItemComponent>();

            factory.Register<SpeciesUI>();
            factory.Register<CharacterInterface>();

            factory.RegisterIgnore("Construction");
            factory.RegisterIgnore("Apc");
            factory.RegisterIgnore("Door");
            factory.RegisterIgnore("PoweredLight");
            factory.RegisterIgnore("Smes");
            factory.RegisterIgnore("Powercell");
            factory.RegisterIgnore("HandheldLight");
            factory.RegisterIgnore("LightBulb");
            factory.RegisterIgnore("Healing");
            factory.RegisterIgnore("Catwalk");
            factory.RegisterIgnore("BallisticMagazine");
            factory.RegisterIgnore("BallisticMagazineWeapon");
            factory.RegisterIgnore("BallisticBullet");
            factory.RegisterIgnore("HitscanWeaponCapacitor");

            prototypes.RegisterIgnore("material");

            factory.RegisterIgnore("PowerCell");

            factory.Register<SharedSpawnPointComponent>();

            factory.Register<SharedLatheComponent>();
            factory.Register<LatheDatabaseComponent>();

            factory.RegisterReference<LatheDatabaseComponent, SharedLatheDatabaseComponent>();

            factory.Register<CameraRecoilComponent>();
            factory.RegisterReference<CameraRecoilComponent, SharedCameraRecoilComponent>();

            factory.Register<SubFloorHideComponent>();

            factory.RegisterIgnore("AiController");
            factory.RegisterIgnore("PlayerInputMover");

            factory.Register<ExaminerComponent>();
            factory.Register<CharacterInfoComponent>();

            factory.Register<WindowComponent>();

            IoCManager.Register<IGameHud, GameHud>();
            IoCManager.Register<IClientNotifyManager, ClientNotifyManager>();
            IoCManager.Register<ISharedNotifyManager, ClientNotifyManager>();
            IoCManager.Register<IClientGameTicker, ClientGameTicker>();
            IoCManager.Register<IParallaxManager, ParallaxManager>();
            IoCManager.Register<IChatManager, ChatManager>();
            IoCManager.Register<IEscapeMenuOwner, EscapeMenuOwner>();
            if (TestingCallbacks != null)
            {
                var cast = (ClientModuleTestingCallbacks) TestingCallbacks;
                cast.ClientBeforeIoC?.Invoke();
            }

            IoCManager.BuildGraph();

            IoCManager.Resolve<IParallaxManager>().LoadParallax();
            IoCManager.Resolve<IBaseClient>().PlayerJoinedServer += SubscribePlayerAttachmentEvents;

            var stylesheet = new NanoStyle();

            IoCManager.Resolve<IUserInterfaceManager>().Stylesheet = stylesheet.Stylesheet;
            IoCManager.Resolve<IUserInterfaceManager>().Stylesheet = stylesheet.Stylesheet;

            IoCManager.InjectDependencies(this);

            _escapeMenuOwner.Initialize();
        }
        /// <summary>
        /// Subscribe events to the player manager after the player manager is set up
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void SubscribePlayerAttachmentEvents(object sender, EventArgs args)
        {
            _playerManager.LocalPlayer.EntityAttached += AttachPlayerToEntity;
            _playerManager.LocalPlayer.EntityDetached += DetachPlayerFromEntity;
        }

        /// <summary>
        /// Add the character interface master which combines all character interfaces into one window
        /// </summary>
        public static void AttachPlayerToEntity(EntityAttachedEventArgs eventArgs)
        {
            eventArgs.NewEntity.AddComponent<CharacterInterface>();
        }

        /// <summary>
        /// Remove the character interface master from this entity now that we have detached ourselves from it
        /// </summary>
        public static void DetachPlayerFromEntity(EntityDetachedEventArgs eventArgs)
        {
            eventArgs.OldEntity.RemoveComponent<CharacterInterface>();
        }

        public override void PostInit()
        {
            base.PostInit();

            // Setup key contexts
            var inputMan = IoCManager.Resolve<IInputManager>();
            ContentContexts.SetupContexts(inputMan.Contexts);

            IoCManager.Resolve<IGameHud>().Initialize();
            IoCManager.Resolve<IClientNotifyManager>().Initialize();
            IoCManager.Resolve<IClientGameTicker>().Initialize();
            IoCManager.Resolve<IOverlayManager>().AddOverlay(new ParallaxOverlay());
            IoCManager.Resolve<IChatManager>().Initialize();
        }

        public override void Update(ModUpdateLevel level, float frameTime)
        {
            base.Update(level, frameTime);

            switch (level)
            {
                case ModUpdateLevel.FramePreEngine:
                    var renderFrameEventArgs = new RenderFrameEventArgs(frameTime);
                    IoCManager.Resolve<IClientNotifyManager>().FrameUpdate(renderFrameEventArgs);
                    IoCManager.Resolve<IClientGameTicker>().FrameUpdate(renderFrameEventArgs);
                    break;
            }
        }
    }
}
