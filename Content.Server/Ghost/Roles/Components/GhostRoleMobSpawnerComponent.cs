using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Ghost.Roles.Components
{
    /// <summary>
    ///   Base class for spawning of an entity for a Ghost
    /// </summary>
    public abstract class GhostRoleSpawnerComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)] [DataField("deleteOnSpawn")]
        public bool DeleteOnSpawn = true;

        [ViewVariables(VVAccess.ReadWrite)] [DataField("availableTakeovers")]
        public int AvailableTakeovers = 1;

        [ViewVariables]
        public int CurrentTakeovers = 0;
    }

    /// <summary>
    ///   Allows a ghost to take this role, spawning a new entity.
    /// </summary>
    [RegisterComponent]
    [Access(typeof(GhostRoleSystem))]
    public sealed class GhostRoleMobSpawnerComponent : GhostRoleSpawnerComponent
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("prototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? Prototype { get; private set; }
    }

    /// <summary>
    ///   Allows a ghost to take this role, spawning a randomized Humanoid
    /// </summary>
    [RegisterComponent]
    [Access(typeof(GhostRoleSystem))]
    public sealed class GhostRoleRandomSpawnerComponent : GhostRoleSpawnerComponent
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("settings", customTypeSerializer: typeof(PrototypeIdSerializer<RandomHumanoidSettingsPrototype>))]
        public string Settings = default!;
    }

}
