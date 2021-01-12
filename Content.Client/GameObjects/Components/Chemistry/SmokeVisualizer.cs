using Content.Shared.GameObjects.Components.Chemistry;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects.Components.Chemistry
{
    public class SmokeVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.Deleted)
            {
                return;
            }

            if (component.TryGetData<Color>(SmokeVisuals.Color, out var color))
            {
                var sprite = component.Owner.GetComponent<ISpriteComponent>();
                sprite.Color = color;
            }
        }
    }
}
