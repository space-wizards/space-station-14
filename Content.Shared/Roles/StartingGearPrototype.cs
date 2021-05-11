#nullable enable
using System.Collections.Generic;
using Content.Shared.Preferences;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using static Content.Shared.GameObjects.Components.Inventory.EquipmentSlotDefines;

namespace Content.Shared.Roles
{
    [Prototype("startingGear")]
    public class StartingGearPrototype : IPrototype
    {
        [DataField("equipment")] private Dictionary<Slots, string> _equipment = new();

        /// <summary>
        /// if empty, there is no skirt override - instead the uniform provided in equipment is added.
        /// </summary>
        [DataField("innerclothingskirt")]
        private string _innerClothingSkirt = default!;

        [DataField("satchel")]
        private string _satchel = string.Empty;

        [DataField("duffelbag")]
        private string _duffelbag = string.Empty;

        public IReadOnlyDictionary<string, string> Inhand => _inHand;
        /// <summary>
        /// hand index, item prototype
        /// </summary>
        [DataField("inhand")]
        private Dictionary<string, string> _inHand = new(0);

        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        public IReadOnlyDictionary<string, string> UniformAlternates => _uniformAlternates;
        [DataField("uniformalternates")]
        private Dictionary<string, string> _uniformAlternates = new(0);

        public string GetGear(Slots slot, HumanoidCharacterProfile? profile, CrewUniformPreference? crewUniformPreference = CrewUniformPreference.Default)
        {
            if (profile != null)
            {
                if (crewUniformPreference != null && crewUniformPreference != CrewUniformPreference.Default)
                {
                    var preference = crewUniformPreference.ToString();
                    if (slot == Slots.INNERCLOTHING && preference != null && _uniformAlternates.ContainsKey(preference))
                    {
                        return _uniformAlternates[preference];
                    }
                }

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
