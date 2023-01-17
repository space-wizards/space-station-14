using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.NPC.Components
{
    [RegisterComponent]
    /// <summary>
    /// Allows clothing to add a faction to you when you wear it.
    /// </summary>
    public sealed class ClothingAddFactionComponent : Component
    {
        public bool IsActive = false;

        /// <summary>
        /// Faction added
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite),
         DataField("faction", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<FactionPrototype>))]
        public string Faction = "";
    }
}
