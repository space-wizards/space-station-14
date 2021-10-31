using System.Collections.Generic;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;
using static Content.Shared.Inventory.EquipmentSlotDefines;

namespace Content.Shared.Roles
{
    [Prototype("startingGear")]
    public class StartingGearPrototype : IPrototype
    {
        // TODO: Custom TypeSerializer for dictionary value prototype IDs
        [DataField("equipment")] private Dictionary<Slots, string> _equipment = new();

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
        [DataField("id", required: true)]
        public string ID { get; } = string.Empty;

        public string GetGear(Slots slot, HumanoidCharacterProfile? profile)
        {
            if (profile != null)
            {
                if (slot == Slots.INNERCLOTHING && profile.Clothing == ClothingPreference.Jumpskirt && !string.IsNullOrEmpty(_innerClothingSkirt))
                    return _innerClothingSkirt;
                if (slot == Slots.BACKPACK && profile.Backpack == BackpackPreference.Satchel && !string.IsNullOrEmpty(_satchel))
                    return _satchel;
                if (slot == Slots.BACKPACK && profile.Backpack == BackpackPreference.Duffelbag && !string.IsNullOrEmpty(_duffelbag))
                    return _duffelbag;
            }

            if (_equipment.ContainsKey(slot))
            {
                return _equipment[slot];
            }
            else
            {
                return "";
            }
        }
    }
}
