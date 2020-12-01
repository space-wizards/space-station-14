using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Botany
{
    [Serializable, NetSerializable]
    public enum PlantHolderVisuals : byte
    {
        Plant,
        HealthLight,
        WaterLight,
        NutritionLight,
        AlertLight,
        HarvestLight,
    }
}
