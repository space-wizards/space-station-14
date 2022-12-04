namespace Content.Server.Salvage.Expeditions.Structure;

/// <summary>
/// Destroy the specified number of structures to finish the expedition.
/// </summary>
[DataDefinition]
public sealed class SalvageStructure : ISalvageMission
{
    /// <summary>
    /// Minimum weight to be used for a wave.
    /// </summary>
    [DataField("minWaveWeight")] public float MinWaveWeight = 5;

    /// <summary>
    /// Minimum time between 2 waves. Roughly the end of one to the start of another.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("waveCooldown")]
    public TimeSpan WaveCooldown = TimeSpan.FromSeconds(30);

    [ViewVariables(VVAccess.ReadWrite), DataField("minStructures")]
    public int MinStructures = 5;

    [ViewVariables(VVAccess.ReadWrite), DataField("maxStructures")]
    public int MaxStructures = 8;

    /// <summary>
    /// How much weight is added for a destroyed structure.
    /// </summary>
    [DataField("minDestroyWeight")]
    public float MinDestroyWeight = 5f;

    /// <summary>
    /// How much weight is added for a destroyed structure.
    /// </summary>
    [DataField("maxDestroyWeight")]
    public float MaxDestroyWeight = 8f;

    /// <summary>
    /// How much weight accumulates per second while the expedition is active.
    /// </summary>
    [DataField("weightAccumulator")]
    public float WeightAccumulator = 0.1f;
}
