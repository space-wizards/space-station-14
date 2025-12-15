using Robust.Shared.Serialization;

namespace Content.Shared.Botany;

[Serializable, NetSerializable]
public enum PlantHolderVisuals
{
    HealthLight,
    WaterLight,
    NutritionLight,
    AlertLight,
    HarvestLight,
}

[Serializable, NetSerializable]
public enum PlantVisuals
{
    PlantRsi,
    PlantState,
}
