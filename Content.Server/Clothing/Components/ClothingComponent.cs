using Content.Server.Hands.Components;
using Content.Shared.Clothing;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Clothing.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedItemComponent))]
    [Virtual]
    public class ItemComponent : SharedItemComponent{}

    [RegisterComponent]
    [NetworkedComponent]
    [ComponentReference(typeof(SharedItemComponent))]
    public sealed class ClothingComponent : ItemComponent, IUse
    {
        [Dependency] private readonly IEntityManager _entities = default!;
        [Dependency] private readonly IPrototypeManager _prototype = default!;

        [DataField("QuickEquip")]
        private bool _quickEquipEnabled = true;

        [DataField("HeatResistance")]
        private int _heatResistance = 323;

        [ViewVariables(VVAccess.ReadWrite)]
        public int HeatResistance => _heatResistance;

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            if (!_quickEquipEnabled) return false;

            var invSystem = EntitySystem.Get<InventorySystem>();
            if (!_entities.TryGetComponent(eventArgs.User, out InventoryComponent? inv)
            ||  !_entities.TryGetComponent(eventArgs.User, out HandsComponent? hands) || !_prototype.TryIndex<InventoryTemplatePrototype>(inv.TemplateId, out var prototype)) return false;

            foreach (var slotDef in prototype.Slots)
            {
                if(!invSystem.CanEquip(eventArgs.User, Owner, slotDef.Name, out _, slotDef, inv))
                    continue;

                if (invSystem.TryGetSlotEntity(eventArgs.User, slotDef.Name, out var slotEntity, inv))
                {
                    if(!invSystem.TryUnequip(eventArgs.User, slotDef.Name, true, inventory: inv))
                        continue;

                    if (!invSystem.TryEquip(eventArgs.User, Owner, slotDef.Name, true, inventory: inv))
                        continue;

                    hands.PutInHandOrDrop(slotEntity.Value);
                }
                else
                {
                    if (!invSystem.TryEquip(eventArgs.User, Owner, slotDef.Name, true, inventory: inv))
                        continue;
                }

                return true;
            }

            return false;
        }
    }
}
