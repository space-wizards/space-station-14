using Content.Client.Botany.Components;
using Content.Shared.Botany;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

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
        sprite.LayerMapReserveBlank(PlantHolderLayers.HealthLight);
        sprite.LayerMapReserveBlank(PlantHolderLayers.WaterLight);
        sprite.LayerMapReserveBlank(PlantHolderLayers.NutritionLight);
        sprite.LayerMapReserveBlank(PlantHolderLayers.AlertLight);
        sprite.LayerMapReserveBlank(PlantHolderLayers.HarvestLight);

        var hydroTools = new ResourcePath("Structures/Hydroponics/overlays.rsi");

        sprite.LayerSetSprite(PlantHolderLayers.HealthLight,
            new SpriteSpecifier.Rsi(hydroTools, "lowhealth3"));
        sprite.LayerSetSprite(PlantHolderLayers.WaterLight,
            new SpriteSpecifier.Rsi(hydroTools, "lowwater3"));
        sprite.LayerSetSprite(PlantHolderLayers.NutritionLight,
            new SpriteSpecifier.Rsi(hydroTools, "lownutri3"));
        sprite.LayerSetSprite(PlantHolderLayers.AlertLight,
            new SpriteSpecifier.Rsi(hydroTools, "alert3"));
        sprite.LayerSetSprite(PlantHolderLayers.HarvestLight,
            new SpriteSpecifier.Rsi(hydroTools, "harvest3"));

        // Let's make those invisible for now.
        sprite.LayerSetVisible(PlantHolderLayers.Plant, false);
        sprite.LayerSetVisible(PlantHolderLayers.HealthLight, false);
        sprite.LayerSetVisible(PlantHolderLayers.WaterLight, false);
        sprite.LayerSetVisible(PlantHolderLayers.NutritionLight, false);
        sprite.LayerSetVisible(PlantHolderLayers.AlertLight, false);
        sprite.LayerSetVisible(PlantHolderLayers.HarvestLight, false);

        // Pretty unshaded lights!
        sprite.LayerSetShader(PlantHolderLayers.HealthLight, "unshaded");
        sprite.LayerSetShader(PlantHolderLayers.WaterLight, "unshaded");
        sprite.LayerSetShader(PlantHolderLayers.NutritionLight, "unshaded");
        sprite.LayerSetShader(PlantHolderLayers.AlertLight, "unshaded");
        sprite.LayerSetShader(PlantHolderLayers.HarvestLight, "unshaded");
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

        if (args.Component.TryGetData<bool>(PlantHolderVisuals.HealthLight, out var health))
        {
            args.Sprite.LayerSetVisible(PlantHolderLayers.HealthLight, health);
        }

        if (args.Component.TryGetData<bool>(PlantHolderVisuals.WaterLight, out var water))
        {
            args.Sprite.LayerSetVisible(PlantHolderLayers.WaterLight, water);
        }

        if (args.Component.TryGetData<bool>(PlantHolderVisuals.NutritionLight, out var nutrition))
        {
            args.Sprite.LayerSetVisible(PlantHolderLayers.NutritionLight, nutrition);
        }

        if (args.Component.TryGetData<bool>(PlantHolderVisuals.AlertLight, out var alert))
        {
            args.Sprite.LayerSetVisible(PlantHolderLayers.AlertLight, alert);
        }

        if (args.Component.TryGetData<bool>(PlantHolderVisuals.HarvestLight, out var harvest))
        {
            args.Sprite.LayerSetVisible(PlantHolderLayers.HarvestLight, harvest);
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
