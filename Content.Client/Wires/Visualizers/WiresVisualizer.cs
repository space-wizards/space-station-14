using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using static Content.Shared.Wires.SharedWiresComponent;

namespace Content.Client.Wires.Visualizers
{
    public class WiresVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<ISpriteComponent>(component.Owner);
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
