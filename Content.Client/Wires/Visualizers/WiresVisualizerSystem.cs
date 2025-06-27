using Content.Shared.Wires;
using Robust.Client.GameObjects;

namespace Content.Client.Wires.Visualizers
{
    public sealed class WiresVisualizerSystem : VisualizerSystem<WiresVisualsComponent>
    {
        [Dependency] private readonly SpriteSystem _sprite = default!;

        protected override void OnAppearanceChange(EntityUid uid, WiresVisualsComponent component, ref AppearanceChangeEvent args)
        {
            if (args.Sprite == null)
                return;

            var layer = _sprite.LayerMapReserve((uid, args.Sprite), WiresVisualLayers.MaintenancePanel);

            if (args.AppearanceData.TryGetValue(WiresVisuals.MaintenancePanelState, out var panelStateObject) &&
                panelStateObject is bool panelState)
            {
                _sprite.LayerSetVisible((uid, args.Sprite), layer, panelState);
            }
            else
            {
                //Mainly for spawn window
                _sprite.LayerSetVisible((uid, args.Sprite), layer, false);
            }
        }
    }

    public enum WiresVisualLayers : byte
    {
        MaintenancePanel
    }
}
