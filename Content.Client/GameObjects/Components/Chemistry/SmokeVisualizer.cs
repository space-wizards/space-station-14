#nullable enable
using Content.Shared.GameObjects.Components.Chemistry;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects.Components.Chemistry
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
