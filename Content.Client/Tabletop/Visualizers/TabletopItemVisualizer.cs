using Content.Shared.Tabletop;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Client.Tabletop.Visualizers
{
    [UsedImplicitly]
    public sealed class TabletopItemVisualizer : AppearanceVisualizer
    {
        [Obsolete("Subscribe to AppearanceChangeEvent instead.")]
        public override void OnChangeData(AppearanceComponent appearance)
        {
            var entities = IoCManager.Resolve<IEntityManager>();
            if (!entities.TryGetComponent<ISpriteComponent>(appearance.Owner, out var sprite))
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
