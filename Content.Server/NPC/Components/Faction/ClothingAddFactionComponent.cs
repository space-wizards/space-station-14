using Content.Server.NPC.Systems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.NPC.Components
{
    /// <summary>
    /// Allows clothing to add a faction to you when you wear it.
    /// </summary>
    [RegisterComponent]
    [Access(typeof(FactionSystem))]
    public sealed class ClothingAddFactionComponent : Component
    {
        /// <summary>
        /// This will skip the active checks so wearing this once permanently adds the faction.
        /// </summary>
        [DataField("persist")]
        public bool Persist = false;

        /// <summary>
        /// Bookkeeping whether this has applied a faction or not.
        /// </summary>
        [ViewVariables]
        public bool IsActive = false;

        /// <summary>
        /// Faction added
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite),
         DataField("faction", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<FactionPrototype>))]
        public string Faction = "";
    }
}
