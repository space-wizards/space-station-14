using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Ghost.Roles.Components
{
    /// <summary>
    ///     Allows a ghost to take this role, spawning a new entity.
    /// </summary>
    [RegisterComponent]
    [Access(typeof(GhostRoleSystem))]
    public sealed partial class GhostRoleMobSpawnerComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)] [DataField("deleteOnSpawn")]
        public bool DeleteOnSpawn = true;

        [ViewVariables(VVAccess.ReadWrite)] [DataField("availableTakeovers")]
        public int AvailableTakeovers = 1;

        [ViewVariables]
        public int CurrentTakeovers = 0;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("prototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? Prototype { get; private set; }
    }
}
