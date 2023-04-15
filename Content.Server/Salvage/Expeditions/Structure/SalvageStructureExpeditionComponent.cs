using Content.Shared.Salvage.Expeditions.Structure;

namespace Content.Server.Salvage.Expeditions.Structure;

/// <summary>
/// Tracks expedition data for <see cref="SalvageStructure"/>
/// </summary>
[RegisterComponent]
public sealed class SalvageStructureExpeditionComponent : Component
{
    [ViewVariables]
    public readonly List<EntityUid> Structures = new();
}
