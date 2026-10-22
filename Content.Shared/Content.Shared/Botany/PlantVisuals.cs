using Robust.Shared.Serialization;

namespace Content.Shared.Botany;

/// <summary>
/// Appearance data keys used by plant tray visualizers.
/// </summary>
[Serializable, NetSerializable]
public enum PlantTrayVisuals
{
    /// <summary>
    /// Whether the plant's health warning light is enabled.
    /// </summary>
    HealthLight,

    /// <summary>
    /// Whether the water warning light is enabled.
    /// </summary>
    WaterLight,

    /// <summary>
    /// Whether the nutrition warning light is enabled.
    /// </summary>
    NutritionLight,

    /// <summary>
    /// Whether the general alert light is enabled.
    /// </summary>
    AlertLight,

    /// <summary>
    /// Whether the plant is ready for harvest.
    /// </summary>
    HarvestLight,
}
