using Robust.Shared.Prototypes;

namespace Content.Server.BluespaceHarvester;

[RegisterComponent]
public sealed partial class BluespaceHarvesterRiftComponent : Component
{
    /// <summary>
    /// The current danger level of the portal with which he will buy things from the Spawn list.
    /// </summary>
    [DataField]
    public int Danger = 0;

    /// <summary>
    /// The portal also periodically generates a random, weak mob from the PassiveSpawn list.
    /// </summary>
    [DataField]
    public float PassiveSpawnCooldown = 30f;

    [DataField]
    public float PassiveSpawnAccumulator = 0f;

    [DataField]
    public float SpawnCooldown = 5f;

    [DataField]
    public float SpawnAccumulator = 0f;

    [DataField]
    public List<EntProtoId> PassiveSpawn = new();

    /// <summary>
    /// Monsters and their cost for purchase through the portal are described here; there may be expensive but very dangerous creatures, for example, kudzu or a dragon.
    /// </summary>
    [DataField]
    public List<EntitySpawn> Spawn = new();
}

[Serializable, DataDefinition]
public partial struct EntitySpawn
{
    [DataField]
    public EntProtoId? Id = null;

    [DataField]
    public int Cost = 1;
}
