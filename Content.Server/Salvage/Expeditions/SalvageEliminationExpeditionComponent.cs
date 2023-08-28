using Content.Shared.Salvage;

namespace Content.Server.Salvage.Expeditions.Structure;

/// <summary>
/// Tracks expedition data for <see cref="SalvageMissionType.Elimination"/>
/// </summary>
[RegisterComponent, Access(typeof(SalvageSystem), typeof(SpawnSalvageMissionJob))]
public sealed partial class SalvageEliminationExpeditionComponent : Component
{
    /// <summary>
    /// List of mobs that need to be killed for the mission to be complete.
    /// </summary>
    [DataField("megafauna")]
    public List<EntityUid> Megafauna = new();
}
