using Content.Shared.Explosion;
using Content.Shared.Inventory;
using Robust.Shared.Containers;

namespace Content.Server.Inventory
{
    public sealed class ServerInventorySystem : InventorySystem
    {
        [Dependency] private readonly SharedContainerSystem _container = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<InventoryComponent, BeforeExplodeEvent>(OnExploded);
        }

        private void OnExploded(Entity<InventoryComponent> ent, ref BeforeExplodeEvent args)
        {
            // explode each item in their inventory too
            var slots = new InventorySlotEnumerator(ent);
            while (slots.MoveNext(out var slot))
            {
                if (slot.ContainedEntity != null)
                    args.Contents.Add(slot.ContainedEntity.Value);
            }
        }

        public void TransferEntityInventories(Entity<InventoryComponent?> source, Entity<InventoryComponent?> target)
        {
            if (!Resolve(source.Owner, ref source.Comp) || !Resolve(target.Owner, ref target.Comp))
                return;

            // Since jumpsuit/clothing is before PDA/pockets in inventory list, this means the enumerator doesn't iterate over
            // those slots at all if they get unequipped. Thus we need to get a list of all the equipped items before we begin to
            // unequip them from the source and equip them to the target.

            var itemList = new List<EntityUid>();
            var slotList = new List<SlotDefinition>();

            var enumerator = new InventorySlotEnumerator(source.Comp);
            while (enumerator.NextItem(out var item, out var slot))
            {
                itemList.Add(item);
                slotList.Add(slot);
            }

            for (var i = 0; i < itemList.Count; i++)
            {
                TryUnequip(source, slotList[i].Name, true, true, inventory: source.Comp);
                TryEquip(target, itemList[i], slotList[i].Name, true, true, inventory: target.Comp);
            }
        }
    }
}
