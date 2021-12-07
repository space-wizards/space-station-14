using Content.Shared.Mining;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Mining
{
    [UsedImplicitly]
    public class AsteroidRockVisualizer : AppearanceVisualizer
    {
        [DataField("layer")]
        private int Layer { get; } = 0;

        public override void InitializeEntity(EntityUid entity)
        {
            base.InitializeEntity(entity);

            IoCManager.Resolve<IEntityManager>().GetComponentOrNull<SpriteComponent>(entity)?.LayerMapReserveBlank(Layer);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(component.Owner, out SpriteComponent? sprite))
            {
                return;
            }

            if (component.TryGetData(AsteroidRockVisuals.State, out string state))
            {
                sprite.LayerMapReserveBlank(Layer);
                sprite.LayerSetState(0, state);
            }
        }
    }
}
