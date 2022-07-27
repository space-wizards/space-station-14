using Content.Shared.Ensnaring;
using Robust.Client.GameObjects;

namespace Content.Client.Ensnaring.Visualizers;

public sealed class EnsnareableVisualizerSystem : VisualizerSystem<EnsnareableVisualizerComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnsnareableVisualizerComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, EnsnareableVisualizerComponent component, ComponentInit args)
    {
        if(!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        sprite.LayerMapReserveBlank(EnsnaredVisualLayers.Ensnared);
    }

    protected override void OnAppearanceChange(EntityUid uid, EnsnareableVisualizerComponent component, ref AppearanceChangeEvent args)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite) && args.Component.TryGetData(EnsnareableVisuals.IsEnsnared, out bool isEnsnared))
        {
            sprite.LayerSetState(EnsnaredVisualLayers.Ensnared, component.State);
            sprite.LayerSetVisible(EnsnaredVisualLayers.Ensnared, isEnsnared);
        }
    }
}

public enum EnsnaredVisualLayers : byte
{
    Ensnared,
}
