using Content.Shared.Inventory;
using Content.Shared.Changeling;

namespace Content.Server.Changeling.Shop
{
    public sealed class ChangelingShopSystem : EntitySystem //mudar tudo
    {
        [Dependency] private readonly InventorySystem _inventorySystem = default!;


        public override void Initialize()
        {
            base.Initialize();

            //SubscribeLocalEvent<ChangelingComponent, ChangelingShopActionEvent>(OnShop);
        }

        private void OnShop(EntityUid uid, ChangelingComponent component, ChangelingShopActionEvent args)
        {
            Console.WriteLine("ATIVOU O IMPLANTEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE");
        }
    }
}
