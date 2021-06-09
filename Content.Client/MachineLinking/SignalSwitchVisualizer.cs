using Content.Shared.GameObjects.Components.MachineLinking;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.GameObjects.Components.MachineLinking
{
    [UsedImplicitly]
    public class SignalSwitchVisualizer : AppearanceVisualizer
    {
        [DataField("layer")]
        private int Layer { get; }

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            if (entity.TryGetComponent(out SpriteComponent? sprite))
            {
                sprite.LayerMapReserveBlank(Layer);
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out SpriteComponent? sprite))
            {
                return;
            }

            if (!component.TryGetData(SignalSwitchVisuals.On, out bool on))
            {
                return;
            }

            sprite.LayerSetState(0, on ? "on" : "off");
        }
    }
}
