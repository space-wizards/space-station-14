using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.BluespaceHarvester;

[RegisterComponent]
public sealed partial class BluespaceHarvesterRiftComponent : Component
{
    [DataField("danger"), ViewVariables(VVAccess.ReadWrite)]
    public int Danger = 0;

    [DataField("passiveSpawnAccumulator"), ViewVariables(VVAccess.ReadWrite)]
    public float PassiveSpawnAccumulator = 30f;

    [DataField("passiveSpawnCooldown"), ViewVariables(VVAccess.ReadWrite)]
    public float PassiveSpawnCooldown = 30f;

    [DataField("passiveSpawn", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public List<string> PassiveSpawnPrototypes = new();

    [DataField("spawn")]
    public List<EntitySpawn> SpawnPrototypes = new();
}

[Serializable, DataDefinition]
public partial struct EntitySpawn
{
    [DataField("id", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string? PrototypeId = null;

    [DataField("cost"), ViewVariables(VVAccess.ReadWrite)]
    public int Cost = 1;
}
