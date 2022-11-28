using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Salvage.Expeditions;

/// <summary>
/// Per-faction config for Salvage Structure expeditions.
/// </summary>
[DataDefinition]
public sealed class SalvageStructureFaction : IFactionExpeditionConfig
{
    [ViewVariables(VVAccess.ReadWrite), DataField("spawn", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Spawn = default!;
}
