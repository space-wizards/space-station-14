using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Disease;

/// <summary>
/// Residual surface/item contamination by diseases.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DiseaseResidueComponent : Component
{
    /// <summary>
    /// Per-disease intensity map for this residue. Each disease ID maps to its current intensity [0..1].
    /// </summary>
    [DataField]
    public Dictionary<string, float> Diseases = [];

    /// <summary>
	/// Intensity decay per tick.
    /// TODO: Must be removed in the future. The premises must be disinfected, not cleaned. (like it was in SS13)
	/// </summary>
	[DataField]
    public float DecayPerTick = 0.08f;

    /// <summary>
    /// Amount to reduce per-disease intensity after a contact interaction.
    /// </summary>
    [DataField]
    public float ContactReduction = 0.15f;
}
