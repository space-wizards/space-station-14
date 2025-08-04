using Content.Shared.Atmos.Monitor;
using Content.Shared.Power;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client.Atmos.Monitor;

public sealed class AtmosAlarmableVisualsSystem : VisualizerSystem<AtmosAlarmableVisualsComponent>
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    protected override void OnAppearanceChange(EntityUid uid, AtmosAlarmableVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null || !_sprite.LayerMapTryGet((uid, args.Sprite), component.LayerMap, out var layer, false))
            return;

        if (!args.AppearanceData.TryGetValue(PowerDeviceVisuals.Powered, out var poweredObject) ||
            poweredObject is not bool powered)
        {
            return;
        }

        if (component.HideOnDepowered != null)
        {
            foreach (var visLayer in component.HideOnDepowered)
            {
                if (_sprite.LayerMapTryGet((uid, args.Sprite), visLayer, out var powerVisibilityLayer, false))
                    _sprite.LayerSetVisible((uid, args.Sprite), powerVisibilityLayer, powered);
            }
        }

        if (component.SetOnDepowered != null && !powered)
        {
            foreach (var (setLayer, powerState) in component.SetOnDepowered)
            {
                if (_sprite.LayerMapTryGet((uid, args.Sprite), setLayer, out var setStateLayer, false))
                    _sprite.LayerSetRsiState((uid, args.Sprite), setStateLayer, new RSI.StateId(powerState));
            }
        }

        if (args.AppearanceData.TryGetValue(AtmosMonitorVisuals.AlarmType, out var alarmTypeObject)
            && alarmTypeObject is AtmosAlarmType alarmType
            && powered
            && component.AlarmStates.TryGetValue(alarmType, out var state))
        {
            _sprite.LayerSetRsiState((uid, args.Sprite), layer, new RSI.StateId(state));
        }
    }
}
