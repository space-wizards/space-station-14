using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Salvage.Expeditions.Structure;

/// <summary>
/// Per-faction config for Salvage Structure expeditions.
/// </summary>
[DataDefinition]
public sealed class SalvageStructureFaction : IFactionExpeditionConfig
{
    /// <summary>
    /// Entity prototype of the structures to destroy.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("spawn", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Spawn = default!;

    /// <summary>
    /// How many groups of mobs to spawn.
    /// </summary>
    [DataField("groupCount")]
    public int Groups = 5;
}
