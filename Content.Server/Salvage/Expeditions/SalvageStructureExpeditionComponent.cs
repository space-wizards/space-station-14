using Content.Shared.Salvage;

namespace Content.Server.Salvage.Expeditions.Structure;

/// <summary>
/// Tracks expedition data for <see cref="SalvageMissionType.Structure"/>
/// </summary>
[RegisterComponent, Access(typeof(SalvageSystem), typeof(SpawnSalvageMissionJob))]
public sealed partial class SalvageStructureExpeditionComponent : Component
{
    [DataField("structures")]
    public List<EntityUid> Structures = new();
}
