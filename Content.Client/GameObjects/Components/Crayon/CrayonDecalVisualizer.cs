using Content.Shared.GameObjects.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects.Components.Crayon
{
    [UsedImplicitly]
    public class CrayonDecalVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var sprite = component.Owner.GetComponent<SpriteComponent>();

            if (component.TryGetData(CrayonVisuals.State, out string state))
            {
                sprite.LayerSetState(0, state);
            }

            if (component.TryGetData(CrayonVisuals.Color, out Color color))
            {
                sprite.LayerSetColor(0, color);
            }

            if (component.TryGetData(CrayonVisuals.Rotation, out Angle rotation))
            {
                sprite.Rotation = rotation;
            }
        }
    }
}
