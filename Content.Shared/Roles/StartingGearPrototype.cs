#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;
using Content.Shared.Preferences;
using static Content.Shared.GameObjects.Components.Inventory.EquipmentSlotDefines;

namespace Content.Shared.Roles
{
    [Prototype("startingGear")]
    public class StartingGearPrototype : IPrototype
    {
        private string _id = default!;
        private Dictionary<Slots, string> _equipment = default!;

        /// <summary>
        /// if empty, there is no skirt override - instead the uniform provided in equipment is added.
        /// </summary>
        private string _innerClothingSkirt = default!;
        private string _satchel = default!;
        private string _duffelbag = default!;

        public IReadOnlyDictionary<string, string> Inhand => _inHand;
        /// <summary>
        /// hand index, item prototype
        /// </summary>
        private Dictionary<string, string> _inHand = default!;

        [ViewVariables] public string ID => _id;

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(ref _id, "id", string.Empty);
            serializer.DataField(ref _inHand, "inhand", new Dictionary<string, string>(0));

            var equipment = serializer.ReadDataField<Dictionary<string, string>>("equipment");

            _equipment = equipment.ToDictionary(slotStr =>
            {
                var (key, _) = slotStr;
                if (!Enum.TryParse(key, true, out Slots slot))
                {
                    throw new Exception($"{key} is an invalid equipment slot.");
                }

                return slot;
            }, type => type.Value);

            serializer.DataField(ref _innerClothingSkirt, "innerclothingskirt", string.Empty);
            serializer.DataField(ref _satchel, "satchel", string.Empty);
            serializer.DataField(ref _duffelbag, "duffelbag", string.Empty);
        }

        public string GetGear(Slots slot, HumanoidCharacterProfile? profile)
        {
            if (profile != null)
            {
                if ((slot == Slots.INNERCLOTHING) && (profile.Clothing == ClothingPreference.Jumpskirt) && (_innerClothingSkirt != ""))
                    return _innerClothingSkirt;
                if ((slot == Slots.BACKPACK) && (profile.Backpack == BackpackPreference.Satchel) && (_satchel != ""))
                    return _satchel;
                if ((slot == Slots.BACKPACK) && (profile.Backpack == BackpackPreference.Duffelbag) && (_duffelbag != ""))
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
