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

        SubscribeLocalEvent<PlantTrayVisualsComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, PlantTrayVisualsComponent component, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        SpriteSystem.LayerMapReserve((uid, sprite), PlantTrayLayers.Plant);
        SpriteSystem.LayerSetVisible((uid, sprite), PlantTrayLayers.Plant, false);
    }

    protected override void OnAppearanceChange(EntityUid uid, PlantTrayVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (AppearanceSystem.TryGetData<string>(uid, PlantVisuals.PlantRsi, out var rsi, args.Component)
            && AppearanceSystem.TryGetData<string>(uid, PlantVisuals.PlantState, out var state, args.Component))
        {
            // Tray should never render plant sprite.
            AppearanceSystem.SetData(uid, PlantVisuals.PlantState, string.Empty, args.Component);

            var valid = !string.IsNullOrWhiteSpace(state);

            SpriteSystem.LayerSetVisible((uid, args.Sprite), PlantTrayLayers.Plant, valid);

            if (valid)
            {
                SpriteSystem.LayerSetRsi((uid, args.Sprite), PlantTrayLayers.Plant, new ResPath(rsi));
                SpriteSystem.LayerSetRsiState((uid, args.Sprite), PlantTrayLayers.Plant, state);
            }
        }
    }
}

public enum PlantTrayLayers : byte
{
    Plant,
    HealthLight,
    WaterLight,
    NutritionLight,
    AlertLight,
    HarvestLight,
}
