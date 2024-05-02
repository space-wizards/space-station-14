using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Robust.Shared.Prototypes;

namespace Content.Shared.Roles
{
    [Prototype("startingGear")]
    public sealed partial class StartingGearPrototype : IPrototype
    {
        /// <summary>
        /// The related loadout that this starting gear has.
        /// </summary>
        [DataField]
        public string Loadout = string.Empty;

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
