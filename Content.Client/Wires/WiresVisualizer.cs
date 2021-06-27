using Robust.Client.GameObjects;
using static Content.Shared.Wires.SharedWiresComponent;

namespace Content.Client.Wires
{
    public class WiresVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            var sprite = component.Owner.GetComponent<ISpriteComponent>();
            if (component.TryGetData<bool>(WiresVisuals.MaintenancePanelState, out var state))
            {
                sprite.LayerSetVisible(WiresVisualLayers.MaintenancePanel, state);
            }
            // Mainly for spawn window
            else
            {
                sprite.LayerSetVisible(WiresVisualLayers.MaintenancePanel, false);
            }
        }

        public enum WiresVisualLayers : byte
        {
            MaintenancePanel,
        }
    }
}
