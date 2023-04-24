namespace Content.Server.Salvage.Expeditions;

/// <summary>
/// Tracks expedition data for <see cref="SalvageMissionType.Mining"/>
/// </summary>
[RegisterComponent, Access(typeof(SalvageSystem))]
public sealed class SalvageMiningExpeditionComponent : Component
{
    /// <summary>
    /// Entities that were present on the shuttle and match the loot tax.
    /// </summary>
    [DataField("exemptEntities")]
    public List<EntityUid> ExemptEntities = new();
}
