using Content.Shared.Wires;
using Robust.Client.GameObjects;

namespace Content.Client.Wires.Visualizers
{
    public sealed class WiresVisualizerSystem : VisualizerSystem<WiresVisualsComponent>
    {
        protected override void OnAppearanceChange(EntityUid uid, WiresVisualsComponent component, ref AppearanceChangeEvent args)
        {
            if (args.Sprite == null)
                return;

            var layer = SpriteSystem.LayerMapReserve((uid, args.Sprite), WiresVisualLayers.MaintenancePanel);

            if (args.AppearanceData.TryGetValue(WiresVisuals.MaintenancePanelState, out var panelStateObject) &&
                panelStateObject is bool panelState)
            {
                SpriteSystem.LayerSetVisible((uid, args.Sprite), layer, panelState);
            }
            else
            {
                //Mainly for spawn window
                SpriteSystem.LayerSetVisible((uid, args.Sprite), layer, false);
            }
        }
    }

    public enum WiresVisualLayers : byte
    {
        MaintenancePanel
    }
}
