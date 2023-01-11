using Content.Shared.Singularity;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Client.Singularity.Visualizers
{
    [UsedImplicitly]
    public sealed class SingularityVisualizer : AppearanceVisualizer
    {
        [DataField("layer")]
        private int Layer { get; } = 0;

        [Obsolete("Subscribe to your component being initialised instead.")]
        public override void InitializeEntity(EntityUid entity)
        {
            base.InitializeEntity(entity);

            IoCManager.Resolve<IEntityManager>().GetComponentOrNull<SpriteComponent>(entity)?.LayerMapReserveBlank(Layer);
        }

        [Obsolete("Subscribe to AppearanceChangeEvent instead.")]
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(component.Owner, out SpriteComponent? sprite))
            {
                return;
            }

            if (!component.TryGetData(SingularityVisuals.Level, out byte level))
            {
                return;
            }

            sprite.LayerSetSprite(Layer, new SpriteSpecifier.Rsi(new ResourcePath("Structures/Power/Generation/Singularity/singularity_" + level + ".rsi"), "singularity_" + level));
        }
    }
}
