using Content.Shared.Labels;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Morgue.Visualizers
{
    [UsedImplicitly]
    public sealed class BodyBagVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var entities = IoCManager.Resolve<IEntityManager>();
            if (!entities.TryGetComponent(component.Owner, out ISpriteComponent? sprite))
            {
                return;
            }

            if (component.TryGetData(PaperLabelVisuals.HasLabel, out bool labelVal))
            {
                sprite.LayerSetVisible(BodyBagVisualLayers.Label, labelVal);
            }
            else
            {
                sprite.LayerSetVisible(BodyBagVisualLayers.Label, false);
            }
        }
    }

    public enum BodyBagVisualLayers : byte
    {
        Label,
    }
}
