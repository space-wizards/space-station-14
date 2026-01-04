using Content.Client.Botany.Components;
using Content.Shared.Botany;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

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

        // Ensure they always render above the tray sprite.
        SpriteSystem.SetDrawDepth((uid, sprite), (int) DrawDepth.SmallObjects);
        SpriteSystem.LayerMapReserve((uid, sprite), PlantLayers.Plant);
        SpriteSystem.LayerSetVisible((uid, sprite), PlantLayers.Plant, false);
    }

    protected override void OnAppearanceChange(EntityUid uid, PlantVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (AppearanceSystem.TryGetData<string>(uid, PlantVisuals.PlantRsi, out var rsi, args.Component)
            && AppearanceSystem.TryGetData<string>(uid, PlantVisuals.PlantState, out var state, args.Component))
        {
            var valid = !string.IsNullOrWhiteSpace(state);

            SpriteSystem.LayerSetVisible((uid, args.Sprite), PlantLayers.Plant, valid);

            if (valid)
            {
                SpriteSystem.LayerSetRsi((uid, args.Sprite), PlantLayers.Plant, new ResPath(rsi));
                SpriteSystem.LayerSetRsiState((uid, args.Sprite), PlantLayers.Plant, state);
            }
        }
    }
}

public enum PlantLayers : byte
{
    Plant
}
