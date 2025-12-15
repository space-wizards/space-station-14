using Content.Client.Botany.Components;
using Content.Shared.Botany;
using Robust.Client.GameObjects;

namespace Content.Client.Botany;

public sealed class PlantVisualizerSystem : VisualizerSystem<PlantVisualsComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlantVisualsComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, PlantVisualsComponent component, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        sprite.LayerMapReserveBlank(PlantLayers.Plant);
        sprite.LayerSetVisible(PlantLayers.Plant, false);
    }

    protected override void OnAppearanceChange(EntityUid uid, PlantVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (AppearanceSystem.TryGetData<string>(uid, PlantVisuals.PlantRsi, out var rsi, args.Component)
            && AppearanceSystem.TryGetData<string>(uid, PlantVisuals.PlantState, out var state, args.Component))
        {
            var valid = !string.IsNullOrWhiteSpace(state);

            args.Sprite.LayerSetVisible(PlantLayers.Plant, valid);

            if (valid)
            {
                args.Sprite.LayerSetRSI(PlantLayers.Plant, rsi);
                args.Sprite.LayerSetState(PlantLayers.Plant, state);
            }
        }
    }
}

public enum PlantLayers : byte
{
    Plant
}
