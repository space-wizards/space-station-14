using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.BluespaceHarvester;

[RegisterComponent]
public sealed partial class BluespaceHarvesterRiftComponent : Component
{
    /// <summary>
    /// The current danger level of the portal with which he will buy things from the Spawn list.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int Danger = 0;

    /// <summary>
    /// The portal also periodically generates a random, weak mob from the PassiveSpawn list.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float PassiveSpawnCooldown = 30f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float PassiveSpawnAccumulator = 0f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
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
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId? Id = null;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int Cost = 1;
}
