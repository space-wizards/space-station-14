using Content.Shared.Preferences;
using Robust.Shared.Prototypes;

namespace Content.Shared.Roles
{
    [Prototype("startingGear")]
    public sealed partial class StartingGearPrototype : IPrototype
    {
        [DataField]
        public Dictionary<string, EntProtoId> Equipment = new();

        [DataField]
        public List<EntProtoId> Inhand = new(0);

        /// <summary>
        /// Inserts entities into the specified slot's storage (if it does have storage).
        /// </summary>
        [DataField]
        public Dictionary<string, List<EntProtoId>> Storage = new();

        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = string.Empty;

        public string GetGear(string slot)
        {
            return Equipment.TryGetValue(slot, out var equipment) ? equipment : string.Empty;
        }
    }
}
