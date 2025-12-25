using Robust.Shared.Serialization;

namespace Content.Shared.Botany;

[Serializable, NetSerializable]
public enum PlantTrayVisuals
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
