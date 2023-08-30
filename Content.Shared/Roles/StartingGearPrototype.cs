using Content.Shared.Preferences;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Roles
{
    [Prototype("startingGear")]
    public sealed class StartingGearPrototype : IPrototype
    {
        [DataField("equipment", customTypeSerializer: typeof(PrototypeIdValueDictionarySerializer<string, EntityPrototype>))]
        private Dictionary<string, string> _equipment = new();

        /// <summary>
        /// if empty, there is no skirt override - instead the uniform provided in equipment is added.
        /// </summary>
        [DataField("innerclothingskirt", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        private string? _innerClothingSkirt;

        [DataField("satchel", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        private string? _satchel;

        [DataField("duffelbag", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        private string? _duffelbag;

        public IReadOnlyDictionary<string, string> Inhand => _inHand;
        /// <summary>
        /// hand index, item prototype
        /// </summary>
        [DataField("inhand")]
        private Dictionary<string, string> _inHand = new(0);

        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = string.Empty;

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
