using Content.Shared.Tabletop;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Maths;

namespace Content.Client.Tabletop.Visualizers
{
    [UsedImplicitly]
    public class TabletopItemVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent appearance)
        {
            if (!appearance.Owner.TryGetComponent<ISpriteComponent>(out var sprite))
            {
                return;
            }

            // TODO: maybe this can work more nicely, by maybe only having to set the item to "being dragged", and have
            //  the appearance handle the rest
            if (appearance.TryGetData<Vector2>(TabletopItemVisuals.Scale, out var scale))
            {
                sprite.Scale = scale;
            }

            if (appearance.TryGetData<int>(TabletopItemVisuals.DrawDepth, out var drawDepth))
            {
                sprite.DrawDepth = drawDepth;
            }
        }
    }
}
