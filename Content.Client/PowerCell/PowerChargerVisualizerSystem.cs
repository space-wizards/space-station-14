using Content.Shared.Power.Components;
using Robust.Client.GameObjects;

namespace Content.Client.PowerCell;

public sealed class PowerChargerVisualizerSystem : VisualizerSystem<PowerChargerVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, PowerChargerVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        // Update base item
        if (AppearanceSystem.TryGetData<bool>(uid, CellVisual.Occupied, out var occupied, args.Component) && occupied)
        {
            // TODO: don't throw if it doesn't have a full state
            SpriteSystem.LayerSetRsiState((uid, args.Sprite), PowerChargerVisualLayers.Base, comp.OccupiedState);
        }
        else
        {
            SpriteSystem.LayerSetRsiState((uid, args.Sprite), PowerChargerVisualLayers.Base, comp.EmptyState);
        }

        // Update lighting
        if (AppearanceSystem.TryGetData<CellChargerStatus>(uid, CellVisual.Light, out var status, args.Component)
            && comp.LightStates.TryGetValue(status, out var lightState))
        {
            SpriteSystem.LayerSetRsiState((uid, args.Sprite), PowerChargerVisualLayers.Light, lightState);
            SpriteSystem.LayerSetVisible((uid, args.Sprite), PowerChargerVisualLayers.Light, true);
        }
        else
            SpriteSystem.LayerSetVisible((uid, args.Sprite), PowerChargerVisualLayers.Light, false);
    }
}

public enum PowerChargerVisualLayers : byte
{
    Base,
    Light,
}
