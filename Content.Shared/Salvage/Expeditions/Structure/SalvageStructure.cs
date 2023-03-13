namespace Content.Shared.Salvage.Expeditions.Structure;

/// <summary>
/// Destroy the specified number of structures to finish the expedition.
/// </summary>
[DataDefinition]
public sealed class SalvageStructure : ISalvageMission
{
    [DataField("desc")]
    public string Description = string.Empty;

    [ViewVariables(VVAccess.ReadWrite), DataField("minStructures")]
    public int MinStructures = 3;

    [ViewVariables(VVAccess.ReadWrite), DataField("maxStructures")]
    public int MaxStructures = 5;
}
