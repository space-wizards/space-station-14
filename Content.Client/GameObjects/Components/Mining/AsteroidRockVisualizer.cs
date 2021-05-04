using Content.Shared.GameObjects.Components.Mining;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.GameObjects.Components.Mining
{
    [UsedImplicitly]
    public class AsteroidRockVisualizer : AppearanceVisualizer
    {
        [DataField("layer")]
        private int Layer { get; } = 0;

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            entity.GetComponentOrNull<SpriteComponent>()?.LayerMapReserveBlank(Layer);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out SpriteComponent? sprite))
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
