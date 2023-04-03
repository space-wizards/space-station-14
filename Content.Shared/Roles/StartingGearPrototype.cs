using Content.Shared.Preferences;
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
        [IdDataField]
        public string ID { get; } = string.Empty;

        public string GetGear(string slot, HumanoidCharacterProfile? profile)
        {
            if (profile != null)
            {
                if (slot == "jumpsuit" && profile.Clothing == ClothingPreference.Jumpskirt && !string.IsNullOrEmpty(_innerClothingSkirt))
                    return _innerClothingSkirt;
                if (slot == "back" && profile.Backpack == BackpackPreference.Satchel && !string.IsNullOrEmpty(_satchel))
                    return _satchel;
                if (slot == "back" && profile.Backpack == BackpackPreference.Duffelbag && !string.IsNullOrEmpty(_duffelbag))
                    return _duffelbag;
            }

            return _equipment.TryGetValue(slot, out var equipment) ? equipment : string.Empty;
        }
    }
}
