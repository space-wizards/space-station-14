using Content.Server.NPC.Systems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.NPC.Components
{
    [RegisterComponent]
    [Access(typeof(FactionSystem))]
    public sealed class FactionComponent : Component
    {
        /// <summary>
        /// Factions this entity is a part of.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite),
         DataField("factions", customTypeSerializer:typeof(PrototypeIdHashSetSerializer<FactionPrototype>))]
        public HashSet<string> Factions = new();

        /// <summary>
        /// Cached friendly factions.
        /// </summary>
        [ViewVariables]
        public readonly HashSet<string> FriendlyFactions = new();

        /// <summary>
        /// Cached hostile factions.
        /// </summary>
        [ViewVariables]
        public readonly HashSet<string> HostileFactions = new();

        /// <summary>
        /// Permanently friendly specific entities. Our summoner, etc.
        /// </summary>
        public HashSet<EntityUid> ExceptionalFriendlies = new();
    }
}
