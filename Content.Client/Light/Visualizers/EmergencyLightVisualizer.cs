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
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var entities = IoCManager.Resolve<IEntityManager>();
            if (!entities.TryGetComponent(component.Owner, out SpriteComponent? sprite))
                return;

            if (!component.TryGetData(EmergencyLightVisuals.On, out bool on))
                on = false;

            sprite.LayerSetState(0, on ? "emergency_light_on" : "emergency_light_off");
        }
    }
}
