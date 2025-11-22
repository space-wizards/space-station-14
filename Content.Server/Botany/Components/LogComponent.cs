using Content.Server.Botany.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Botany.Components;
// TODO: This should probably be merged with SliceableFood somehow or made into a more generic Choppable.
// Yeah this is pretty trash. also consolidating this type of behavior will avoid future transform parenting bugs (see #6090).

[RegisterComponent]
[Access(typeof(LogSystem))]
public sealed partial class LogComponent : Component
{
    /// <summary>
    /// Prototype ID of the entity spawned when this log is chopped.
    /// </summary>
    [DataField("spawnedPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SpawnedPrototype = "MaterialWoodPlank1";

    /// <summary>
    /// Number of entities spawned when this log is chopped.
    /// </summary>
    [DataField("spawnCount")] public int SpawnCount = 2;
}
