using Content.Server.GameObjects;
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

            factory.Register<DamageableComponent>();
            factory.Register<DestructibleComponent>();

            factory.Register<TemperatureComponent>();
        }
    }
}
