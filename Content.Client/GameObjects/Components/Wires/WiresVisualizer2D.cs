using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using static Content.Shared.GameObjects.Components.SharedWiresComponent;

namespace Content.Client.GameObjects.Components
{
    public class WiresVisualizer2D : AppearanceVisualizer
    {
        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            var sprite = entity.GetComponent<ISpriteComponent>();
            sprite.LayerSetTexture(WiresVisualLayers.MaintenancePanel, "/Textures/Buildings/maintenance_panel.png");
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var sprite = component.Owner.GetComponent<ISpriteComponent>();
            if (component.TryGetData<bool>(WiresVisuals.MaintenancePanelState, out var state))
            {
                sprite.LayerSetVisible(WiresVisualLayers.MaintenancePanel, state);
            }
        }

        public enum WiresVisualLayers
        {
            MaintenancePanel,
        }
    }
}
