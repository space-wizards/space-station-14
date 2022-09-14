using Content.Shared.PneumaticCannon;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.PneumaticCannon
{
    public sealed class PneumaticCannonVisualizer : AppearanceVisualizer
    {
        [Obsolete("Subscribe to AppearanceChangeEvent instead.")]
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var entities = IoCManager.Resolve<IEntityManager>();
            if (!entities.TryGetComponent(component.Owner, out SpriteComponent? sprite))
                return;

            if (component.TryGetData(PneumaticCannonVisuals.Tank, out bool tank))
            {
                sprite.LayerSetVisible(PneumaticCannonVisualLayers.Tank, tank);
            }
        }
    }
}
