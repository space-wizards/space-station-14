using Content.Shared.Smoking;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Client.Chemistry.Visualizers
{
    [UsedImplicitly]
    public sealed class SmokeVisualizer : AppearanceVisualizer
    {
        [Obsolete("Subscribe to AppearanceChangeEvent instead.")]
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var entities = IoCManager.Resolve<IEntityManager>();
            if (component.TryGetData<Color>(SmokeVisuals.Color, out var color))
            {
                if (entities.TryGetComponent(component.Owner, out ISpriteComponent? sprite))
                {
                    sprite.Color = color;
                }
            }
        }
    }
}
