using Content.Shared.Smoking;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Maths;

namespace Content.Client.Chemistry.Visualizers
{
    [UsedImplicitly]
    public class SmokeVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.TryGetData<Color>(SmokeVisuals.Color, out var color))
            {
                if (component.Owner.TryGetComponent(out ISpriteComponent? sprite))
                {
                    sprite.Color = color;
                }
            }
        }
    }
}
