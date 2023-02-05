namespace Content.Shared.Salvage.Expeditions.Extraction;

public sealed class SalvageExtraction : ISalvageMission
{
    /// <summary>
    /// Minimum weight to be used for a wave.
    /// </summary>
    [DataField("minWaveWeight")] public float MinWaveWeight = 5;

    /// <summary>
    /// Minimum time between 2 waves. Roughly the end of one to the start of another.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("waveCooldown")]
    public TimeSpan WaveCooldown = TimeSpan.FromSeconds(60);

    /// <summary>
    /// How much weight accumulates per second while the expedition is active.
    /// </summary>
    [DataField("weightAccumulator")]
    public float WeightAccumulator = 0.1f;
}
