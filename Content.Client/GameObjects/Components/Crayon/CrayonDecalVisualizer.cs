using Content.Shared.GameObjects.Components;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects.Components.Crayon
{
    public class CrayonDecalVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var sprite = component.Owner.GetComponent<SpriteComponent>();

            var state = component.GetData<string>(CrayonVisuals.State);
            var color = component.GetData<Color>(CrayonVisuals.Color);
            var rotation = component.GetData<Angle>(CrayonVisuals.Rotation);

            sprite.LayerSetState(0, state);
            sprite.LayerSetColor(0, color);
            sprite.Rotation = rotation;
        }
    }
}
