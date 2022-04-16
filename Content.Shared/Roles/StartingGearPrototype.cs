using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Preferences;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Roles
{
    [Prototype("startingGear")]
    public sealed class StartingGearPrototype : IPrototype
    {
        // TODO: Custom TypeSerializer for dictionary value prototype IDs
        [DataField("equipment")] private Dictionary<string, string> _equipment = new();

        /// <summary>
        /// if empty, there is no skirt override - instead the uniform provided in equipment is added.
        /// </summary>
        [DataField("innerclothingskirt", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        private string _innerClothingSkirt = string.Empty;

        [DataField("satchel", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        private string _satchel = string.Empty;

        [DataField("duffelbag", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        private string _duffelbag = string.Empty;

        public IReadOnlyDictionary<string, string> Inhand => _inHand;
        /// <summary>
        /// hand index, item prototype
        /// </summary>
        [DataField("inhand")]
        private Dictionary<string, string> _inHand = new(0);

        [ViewVariables]
        [IdDataFieldAttribute]
        public string ID { get; } = string.Empty;

        public void EquipStartingGear(EntityUid uid, HumanoidCharacterProfile? profile = null, UnequipMethod stripMethod = UnequipMethod.None, bool force = false, InventorySystem? inventorySystem = null, SharedHandsSystem? handsSystem = null, IEntityManager? entityManager = null, IEntitySystemManager? systemManager = null)
        {
            IoCManager.Resolve(ref entityManager);
            if (inventorySystem == null || handsSystem == null)
            {
                IoCManager.Resolve(ref systemManager);
                systemManager.Resolve(ref inventorySystem, ref handsSystem);
            }

            var slotQueue = new Queue<(string slot, string equipment)>(GetGear(profile));
            string? latestDeferredSlot = null;

            // todo spawn in nullspace as soon as sloth fixes it
            var mapCoords = entityManager.GetComponent<TransformComponent>(uid).MapPosition;

            while (slotQueue.TryDequeue(out var slotEquipment))
            {
                if (!inventorySystem.HasSlot(uid, slotEquipment.slot))
                {
                    if (latestDeferredSlot == slotEquipment.slot)
                    {
                        Logger.Error($"Could not find slot {slotEquipment.slot} to equip {slotEquipment.equipment}.");
                        continue;
                    }

                    latestDeferredSlot = slotEquipment.slot;
                    slotQueue.Enqueue(slotEquipment);
                }

                if (stripMethod != UnequipMethod.None && inventorySystem.TryUnequip(uid, slotEquipment.slot, out var unequippedItem, true, true))
                {
                    if(stripMethod == UnequipMethod.StripDelete)
                        entityManager.DeleteEntity(unequippedItem.Value);
                }

                var item = entityManager.SpawnEntity(slotEquipment.equipment, mapCoords);
                inventorySystem.TryEquip(uid, item, slotEquipment.slot, true, force);
            }

            if (!entityManager.TryGetComponent(uid, out SharedHandsComponent? handsComponent))
                return;

            foreach (var (hand, prototype) in _inHand)
            {
                if (stripMethod != UnequipMethod.None && handsComponent.Hands.TryGetValue(hand, out var handObj) && handObj.HeldEntity.HasValue)
                {
                    handsSystem.TryDrop(uid, handObj, checkActionBlocker: false, doDropInteraction: false, handsComp: handsComponent);
                    if(stripMethod == UnequipMethod.StripDelete)
                        entityManager.DeleteEntity(handObj.HeldEntity.Value);
                }

                var inhandEntity = entityManager.SpawnEntity(prototype, mapCoords);
                handsSystem.TryPickup(uid, inhandEntity, hand, checkActionBlocker: false, handsComp: handsComponent);
            }
        }

        public IEnumerable<(string slot, string equipment)> GetGear(HumanoidCharacterProfile? profile)
        {
            foreach (var (slot, prototypeId) in _equipment)
            {
                yield return slot switch
                {
                    "jumpsuit" => (slot,
                        profile?.Clothing == ClothingPreference.Jumpskirt && !string.IsNullOrEmpty(_innerClothingSkirt)
                            ? _innerClothingSkirt
                            : prototypeId),
                    "back" => (slot, profile?.Backpack switch
                    {
                        BackpackPreference.Duffelbag when !string.IsNullOrEmpty(_duffelbag) => _duffelbag,
                        BackpackPreference.Satchel when !string.IsNullOrEmpty(_satchel) => _satchel,
                        _ => prototypeId
                    }),
                    _ => (slot, prototypeId)
                };
            }
        }

        public enum UnequipMethod : byte
        {
            None,
            Strip,
            StripDelete
        }
    }
}
