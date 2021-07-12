using Content.Shared.Power;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Power
{
    [UsedImplicitly]
    public class PowerDeviceVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var sprite = component.Owner.GetComponent<ISpriteComponent>();
            var powered = component.TryGetData(PowerDeviceVisuals.Powered, out bool poweredVar) && poweredVar;
            sprite.LayerSetVisible(PowerDeviceVisualLayers.Powered, powered);
        }
    }

    public enum PowerDeviceVisualLayers : byte
    {
        Powered
    }
}
