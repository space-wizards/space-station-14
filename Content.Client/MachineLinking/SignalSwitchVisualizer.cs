using Content.Shared.MachineLinking;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.MachineLinking
{
    [UsedImplicitly]
    public sealed class SignalSwitchVisualizer : AppearanceVisualizer
    {
        [DataField("layer")]
        private int Layer { get; }

        [Obsolete("Subscribe to your component being initialised instead.")]
        public override void InitializeEntity(EntityUid entity)
        {
            base.InitializeEntity(entity);

            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out SpriteComponent? sprite))
            {
                sprite.LayerMapReserveBlank(Layer);
            }
        }

        [Obsolete("Subscribe to AppearanceChangeEvent instead.")]
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var entities = IoCManager.Resolve<IEntityManager>();
            if (!entities.TryGetComponent(component.Owner, out SpriteComponent? sprite))
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
