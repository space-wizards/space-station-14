using Content.Client.Botany.Components;
using Content.Shared.Botany;
using Robust.Client.GameObjects;

namespace Content.Client.Botany;

public sealed class PlantHolderVisualizerSystem : VisualizerSystem<PlantHolderVisualsComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlantHolderVisualsComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, PlantHolderVisualsComponent component, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        sprite.LayerMapReserveBlank(PlantHolderLayers.Plant);
        sprite.LayerSetVisible(PlantHolderLayers.Plant, false);
    }

    protected override void OnAppearanceChange(EntityUid uid, PlantHolderVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (args.Component.TryGetData<string>(PlantHolderVisuals.PlantRsi, out var rsi)
            && args.Component.TryGetData<string>(PlantHolderVisuals.PlantState, out var state))
        {
            var valid = !string.IsNullOrWhiteSpace(state);

            args.Sprite.LayerSetVisible(PlantHolderLayers.Plant, valid);

            if (valid)
            {
                args.Sprite.LayerSetRSI(PlantHolderLayers.Plant, rsi);
                args.Sprite.LayerSetState(PlantHolderLayers.Plant, state);
            }
        }
    }
}

public enum PlantHolderLayers : byte
{
    Plant,
    HealthLight,
    WaterLight,
    NutritionLight,
    AlertLight,
    HarvestLight,
}
