using Content.Shared.Atmos.Visuals;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Atmos.Visualizers
{
    [UsedImplicitly]
    public sealed class AtmosPlaqueVisualizer : AppearanceVisualizer
    {
        [DataField("layer")]
        private int Layer { get; }

        public override void InitializeEntity(EntityUid entity)
        {
            base.InitializeEntity(entity);

            IoCManager.Resolve<IEntityManager>().GetComponentOrNull<SpriteComponent>(entity);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(component.Owner, out SpriteComponent? sprite))
            {
                return;
            }

            if (!component.TryGetData(AtmosPlaqueVisuals.State, out string state))
            {
                sprite.LayerSetState(Layer, state);
            }
        }
    }
}
