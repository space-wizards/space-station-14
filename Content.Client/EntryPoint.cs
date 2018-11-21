using Content.Client.GameObjects;
using Content.Client.GameObjects.Components.Clothing;
using Content.Client.GameObjects.Components.Construction;
using Content.Client.GameObjects.Components.Power;
using Content.Client.GameObjects.Components.SmoothWalling;
using Content.Client.GameObjects.Components.Storage;
using Content.Client.Input;
using Content.Client.Interfaces;
using Content.Client.Interfaces.GameObjects;
using Content.Shared.Interfaces;
using SS14.Client;
using SS14.Client.Interfaces.Input;
using SS14.Client.Utility;
using SS14.Shared.ContentPack;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Prototypes;

namespace Content.Client
{
    public class EntryPoint : GameClient
    {
        public override void Init()
        {
#if DEBUG
            GodotResourceCopy.DoDirCopy("../../Resources", "Content");
#endif
            var factory = IoCManager.Resolve<IComponentFactory>();
            var prototypes = IoCManager.Resolve<IPrototypeManager>();

            factory.RegisterIgnore("Interactable");
            factory.RegisterIgnore("Damageable");
            factory.RegisterIgnore("Destructible");
            factory.RegisterIgnore("Temperature");
            factory.RegisterIgnore("PowerTransfer");
            factory.RegisterIgnore("PowerNode");
            factory.RegisterIgnore("PowerProvider");
            factory.RegisterIgnore("PowerDevice");
            factory.RegisterIgnore("PowerStorage");
            factory.RegisterIgnore("PowerGenerator");

            factory.RegisterIgnore("Wirecutter");
            factory.RegisterIgnore("Screwdriver");
            factory.RegisterIgnore("Multitool");
            factory.RegisterIgnore("Welder");
            factory.RegisterIgnore("Wrench");
            factory.RegisterIgnore("Crowbar");
            factory.RegisterIgnore("HitscanWeapon");
            factory.RegisterIgnore("ProjectileWeapon");
            factory.RegisterIgnore("Projectile");
            factory.RegisterIgnore("MeleeWeapon");

            factory.RegisterIgnore("Storeable");

            factory.RegisterIgnore("Material");
            factory.RegisterIgnore("Stack");

            factory.Register<HandsComponent>();
            factory.RegisterReference<HandsComponent, IHandsComponent>();
            factory.Register<ClientStorageComponent>();
            factory.Register<ClientInventoryComponent>();
            factory.Register<PowerDebugTool>();
            factory.Register<ConstructorComponent>();
            factory.Register<ConstructionGhostComponent>();
            factory.Register<IconSmoothComponent>();
            factory.Register<ClothingComponent>();
            factory.Register<ItemComponent>();
            factory.RegisterReference<ClothingComponent, ItemComponent>();

            factory.RegisterIgnore("Construction");
            factory.RegisterIgnore("Apc");
            factory.RegisterIgnore("Door");
            factory.RegisterIgnore("PoweredLight");
            factory.RegisterIgnore("Smes");

            prototypes.RegisterIgnore("material");

            IoCManager.Register<IClientNotifyManager, ClientNotifyManager>();
            IoCManager.Register<ISharedNotifyManager, ClientNotifyManager>();
            IoCManager.BuildGraph();
        }

        public override void PostInit()
        {
            base.PostInit();

            // Setup key contexts
            var inputMan = IoCManager.Resolve<IInputManager>();
            ContentContexts.SetupContexts(inputMan.Contexts);

            IoCManager.Resolve<IClientNotifyManager>().Initialize();
        }

        public override void Update(AssemblyLoader.UpdateLevel level, float frameTime)
        {
            base.Update(level, frameTime);

            switch (level)
            {
                case AssemblyLoader.UpdateLevel.FramePreEngine:
                    IoCManager.Resolve<IClientNotifyManager>().FrameUpdate(new RenderFrameEventArgs(frameTime));
                    break;
            }
        }
    }
}
