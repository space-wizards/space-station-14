#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;
using Content.Shared.Preferences;
using Robust.Shared.Serialization.Manager.Attributes;
using static Content.Shared.GameObjects.Components.Inventory.EquipmentSlotDefines;

namespace Content.Shared.Roles
{
    [Prototype("startingGear")]
    public class StartingGearPrototype : IPrototype, IIndexedPrototype
    {
        [YamlField("id")]
        private string _id = string.Empty;

        [YamlField("equipment")] private Dictionary<Slots, string> _equipment = new();

        /// <summary>
        /// if empty, there is no skirt override - instead the uniform provided in equipment is added.
        /// </summary>
        [YamlField("innerclothingskirt")]
        private string _innerClothingSkirt = default!;

        public IReadOnlyDictionary<string, string> Inhand => _inHand;
        /// <summary>
        /// hand index, item prototype
        /// </summary>
        [YamlField("inhand")]
        private Dictionary<string, string> _inHand = new(0);

        [ViewVariables] public string ID => _id;

        public string GetGear(Slots slot, HumanoidCharacterProfile? profile)
        {
            if (profile != null)
            {
                if ((slot == Slots.INNERCLOTHING) && (profile.Clothing == ClothingPreference.Jumpskirt) && (_innerClothingSkirt != ""))
                    return _innerClothingSkirt;
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
