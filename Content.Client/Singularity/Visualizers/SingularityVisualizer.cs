using Content.Shared.Singularity;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Singularity.Visualizers
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

            sprite.LayerSetRSI(Layer, "Structures/Power/Generation/Singularity/singularity_" + level + ".rsi");
            sprite.LayerSetState(Layer, "singularity_" + level);
        }
    }
}
