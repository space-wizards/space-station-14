using Content.Client.Botany.Components;
using Content.Shared.Botany;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client.Botany;

public sealed class PlantTrayVisualizerSystem : VisualizerSystem<PlantTrayVisualsComponent>
{
    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void OnAppearanceChange(EntityUid uid, PlantTrayVisualsComponent component, ref AppearanceChangeEvent args)
    {

    }
}

public enum PlantTrayLayers : byte
{
    HealthLight,
    WaterLight,
    NutritionLight,
    AlertLight,
    HarvestLight,
}
