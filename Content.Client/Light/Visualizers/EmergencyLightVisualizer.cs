using Content.Shared.Light.Component;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Light.Visualizers
{
    [DataDefinition]
    public sealed class EmergencyLightVisualizer : AppearanceVisualizer
    {
        [Obsolete("Subscribe to AppearanceChangeEvent instead.")]
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var entities = IoCManager.Resolve<IEntityManager>();
            if (!entities.TryGetComponent(component.Owner, out SpriteComponent? sprite))
                return;

            if (!component.TryGetData<bool>(EmergencyLightVisuals.On, out var on))
                on = false;

            sprite.LayerSetState(EmergencyLightVisualLayers.Light, on ? "emergency_light_on" : "emergency_light_off");
            sprite.LayerSetShader(EmergencyLightVisualLayers.Light, on ? "unshaded" : "shaded");

            if (component.TryGetData<Color>(EmergencyLightVisuals.Color, out var color))
            {
                sprite.LayerSetColor(EmergencyLightVisualLayers.Light, color);
            }
        }
    }
}

public enum EmergencyLightVisualLayers
{
    Base,
    Light
}
