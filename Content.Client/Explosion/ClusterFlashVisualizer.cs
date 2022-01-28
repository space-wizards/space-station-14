using Content.Shared.Explosion;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Explosion
{
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    public class ClusterFlashVisualizer : AppearanceVisualizer
    {
        [DataField("state")]
        private string? _state;

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var entities = IoCManager.Resolve<IEntityManager>();
            if (!entities.TryGetComponent<ISpriteComponent>(component.Owner, out var sprite))
            {
                return;
            }

            if (component.TryGetData(ClusterFlashVisuals.GrenadesCounter, out int grenadesCounter))
            {
                sprite.LayerSetState(0, $"{_state}-{grenadesCounter}");
            }
        }
    }
}
