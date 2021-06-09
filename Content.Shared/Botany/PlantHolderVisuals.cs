#nullable enable
using System;
using Robust.Shared.Serialization;

namespace Content.Shared.Botany
{
    [Serializable, NetSerializable]
    public enum PlantHolderVisuals
    {
        Plant,
        HealthLight,
        WaterLight,
        NutritionLight,
        AlertLight,
        HarvestLight,
    }
}
