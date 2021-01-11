using Content.Shared.GameObjects.Components.Power;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;

namespace Content.Client.GameObjects.Components.Power
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
