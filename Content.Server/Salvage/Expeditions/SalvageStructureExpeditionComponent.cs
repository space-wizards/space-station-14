using Content.Shared.Salvage;

namespace Content.Server.Salvage.Expeditions.Structure;

/// <summary>
/// Tracks expedition data for <see cref="SalvageMissionType.Structure"/>
/// </summary>
[RegisterComponent, Access(typeof(SalvageSystem), typeof(SpawnSalvageMissionJob))]
public sealed class SalvageStructureExpeditionComponent : Component
{
    [DataField("structures")]
    public readonly List<EntityUid> Structures = new();
}
