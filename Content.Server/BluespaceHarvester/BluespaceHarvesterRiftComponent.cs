using Robust.Shared.Prototypes;

namespace Content.Server.BluespaceHarvester;

[RegisterComponent]
public sealed partial class BluespaceHarvesterRiftComponent : Component
{
    /// <summary>
    /// The current danger level of the portal with which he will buy things from the Spawn list.
    /// </summary>
    [DataField]
    public int Danger;

    /// <summary>
    /// The portal also periodically generates a random, weak mob from the PassiveSpawn list.
    /// </summary>
    [DataField]
    public float PassiveSpawnCooldown = 30f;

    [DataField]
    public float PassiveSpawnAccumulator;

    /// <summary>
    /// A list of weak monsters that will encourage breaking rifts.
    /// </summary>
    [DataField]
    public List<EntProtoId> PassiveSpawn = new();

    /// <summary>
    /// Delay between attempts to spawn more than 3 mobs.
    /// </summary>
    [DataField]
    public float SpawnCooldown = 5f;

    [DataField]
    public float SpawnAccumulator;

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
    public int Cost;
}
