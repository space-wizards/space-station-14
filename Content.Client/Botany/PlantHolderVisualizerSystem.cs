using Content.Client.Botany.Components;
using Content.Shared.Botany;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client.Botany;

public sealed class PlantHolderVisualizerSystem : VisualizerSystem<PlantHolderVisualsComponent>
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlantHolderVisualsComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, PlantHolderVisualsComponent component, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        _sprite.LayerMapReserve((uid, sprite), PlantHolderLayers.Plant);
        _sprite.LayerSetVisible((uid, sprite), PlantHolderLayers.Plant, false);
    }

    protected override void OnAppearanceChange(EntityUid uid, PlantHolderVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (AppearanceSystem.TryGetData<string>(uid, PlantHolderVisuals.PlantRsi, out var rsi, args.Component)
            && AppearanceSystem.TryGetData<string>(uid, PlantHolderVisuals.PlantState, out var state, args.Component))
        {
            var valid = !string.IsNullOrWhiteSpace(state);

            _sprite.LayerSetVisible((uid, args.Sprite), PlantHolderLayers.Plant, valid);

            if (valid)
            {
                _sprite.LayerSetRsi((uid, args.Sprite), PlantHolderLayers.Plant, new ResPath(rsi));
                _sprite.LayerSetRsiState((uid, args.Sprite), PlantHolderLayers.Plant, state);
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
