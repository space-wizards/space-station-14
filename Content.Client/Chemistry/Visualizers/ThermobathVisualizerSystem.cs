using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Temperature.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Chemistry.Visualizers;

public sealed partial class ThermobathVisualizerSystem : VisualizerSystem<ThermobathComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, ThermobathComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        AppearanceSystem.TryGetData(uid, ThermobathVisuals.Powered, out bool powered, args.Component);
        AppearanceSystem.TryGetData(uid, ThermobathVisuals.HasBeaker, out bool hasBeaker, args.Component);
        AppearanceSystem.TryGetData(uid, ThermobathVisuals.ActiveMode, out ThermoregulatorActiveMode activeMode, args.Component);

        var heating = powered && activeMode == ThermoregulatorActiveMode.Heating;
        var cooling = powered && activeMode == ThermoregulatorActiveMode.Cooling;

        SetVisible(uid, args.Sprite, ThermobathVisualLayers.PowerOn, powered);
        SetVisible(uid, args.Sprite, ThermobathVisualLayers.PowerOff, !powered);
        SetVisible(uid, args.Sprite, ThermobathVisualLayers.Heating, heating);
        SetVisible(uid, args.Sprite, ThermobathVisualLayers.Cooling, cooling);
        SetVisible(uid, args.Sprite, ThermobathVisualLayers.Open, !hasBeaker);
        SetVisible(uid, args.Sprite, ThermobathVisualLayers.Beaker, hasBeaker);
        SetVisible(uid, args.Sprite, ThermobathVisualLayers.LidIdle, hasBeaker && activeMode == ThermoregulatorActiveMode.Idle);
        SetVisible(uid, args.Sprite, ThermobathVisualLayers.LidCooling, hasBeaker && cooling);
        SetVisible(uid, args.Sprite, ThermobathVisualLayers.LidHeating, hasBeaker && heating);
    }

    private void SetVisible(EntityUid uid, SpriteComponent sprite, ThermobathVisualLayers layer, bool visible)
    {
        if (SpriteSystem.LayerMapTryGet((uid, sprite), layer, out var index, false))
            SpriteSystem.LayerSetVisible((uid, sprite), index, visible);
    }
}
