using Content.Shared.PneumaticCannon;
using Robust.Client.GameObjects;

namespace Content.Client.PneumaticCannon
{
    public class PneumaticCannonVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent<SpriteComponent>(out var sprite))
                return;

            if (component.TryGetData(PneumaticCannonVisuals.Tank, out bool tank))
            {
                sprite.LayerSetVisible(PneumaticCannonVisualLayers.Tank, tank);
            }
        }
    }
}
