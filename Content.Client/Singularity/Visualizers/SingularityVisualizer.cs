using Content.Shared.GameObjects.Components.Singularity;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.GameObjects.Components.Singularity
{
    [UsedImplicitly]
    public class SingularityVisualizer : AppearanceVisualizer
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

            if (!component.TryGetData(SingularityVisuals.Level, out int level))
            {
                return;
            }

            sprite.LayerSetRSI(Layer, "Constructible/Power/Singularity/singularity_" + level + ".rsi");
            sprite.LayerSetState(Layer, "singularity_" + level);
        }
    }
}
