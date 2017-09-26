using SS14.Shared.ContentPack;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;

namespace Content.Client
{
    public class EntryPoint : GameClient
    {
        public override void Init()
        {
            var factory = IoCManager.Resolve<IComponentFactory>();

            factory.RegisterIgnore("Inventory");
            factory.RegisterIgnore("Item");
        }
    }
}
