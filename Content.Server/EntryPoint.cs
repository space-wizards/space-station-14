using Content.Server.GameObjects;
using Content.Server.Interfaces.GameObjects;
using SS14.Shared.ContentPack;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;

namespace Content.Server
{
    public class EntryPoint : GameServer
    {
        public override void Init()
        {
            var factory = IoCManager.Resolve<IComponentFactory>();

            factory.Register<HandsComponent>();
            factory.RegisterReference<HandsComponent, IHandsComponent>();

            factory.Register<InventoryComponent>();
            factory.RegisterReference<InventoryComponent, IInventoryComponent>();

            factory.Register<ItemComponent>();
            factory.RegisterReference<ItemComponent, IItemComponent>();

            factory.Register<InteractableComponent>();
            factory.RegisterReference<InteractableComponent, IInteractableComponent>();

            factory.Register<DamageableComponent>();
            factory.Register<DestructibleComponent>();
            factory.Register<TemperatureComponent>();
        }
    }
}
