using Content.Shared.Power;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Power
{
    [UsedImplicitly]
    public sealed class PowerDeviceVisualizer : AppearanceVisualizer
    {
        [Obsolete("Subscribe to AppearanceChangeEvent instead.")]
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<SpriteComponent>(component.Owner);
            var powered = component.TryGetData(PowerDeviceVisuals.Powered, out bool poweredVar) && poweredVar;
            sprite.LayerSetVisible(PowerDeviceVisualLayers.Powered, powered);
        }
    }

    public enum PowerDeviceVisualLayers : byte
    {
        Powered
    }
}
