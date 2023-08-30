using Content.Shared.Ensnaring;
using Content.Shared.Ensnaring.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Ensnaring.Visualizers;

public sealed class EnsnareableSystem : SharedEnsnareableSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnsnareableComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<EnsnareableComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnComponentInit(EntityUid uid, EnsnareableComponent component, ComponentInit args)
    {
        if(!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        // TODO remove this, this should just be in yaml.
        sprite.LayerMapReserveBlank(EnsnaredVisualLayers.Ensnared);
    }

    private void OnAppearanceChange(EntityUid uid, EnsnareableComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null || !args.Sprite.LayerMapTryGet(EnsnaredVisualLayers.Ensnared, out var layer))
            return;

        if (_appearance.TryGetData<bool>(uid, EnsnareableVisuals.IsEnsnared, out var isEnsnared, args.Component))
        {
            if (component.Sprite != null)
            {
                args.Sprite.LayerSetRSI(layer, component.Sprite);
                args.Sprite.LayerSetState(layer, component.State);
                args.Sprite.LayerSetVisible(layer, isEnsnared);
            }
        }
    }
}

public enum EnsnaredVisualLayers : byte
{
    Ensnared,
}
