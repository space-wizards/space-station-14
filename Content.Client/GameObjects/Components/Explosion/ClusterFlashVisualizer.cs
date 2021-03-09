using Content.Shared.GameObjects.Components.Explosion;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Explosion
{
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    public class ClusterFlashVisualizer : AppearanceVisualizer
    {
        [DataField("state")]
        private string _state;

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent<ISpriteComponent>(out var sprite))
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
