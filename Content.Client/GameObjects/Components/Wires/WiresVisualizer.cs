using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using static Content.Shared.GameObjects.Components.SharedWiresComponent;

namespace Content.Client.GameObjects.Components.Wires
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
