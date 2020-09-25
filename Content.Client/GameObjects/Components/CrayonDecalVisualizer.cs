using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Maths;
using System;
using System.Collections.Generic;
using System.Text;
using static Content.Shared.GameObjects.Components.SharedCrayonComponent;

namespace Content.Client.GameObjects.Components
{
    public class CrayonDecalVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var sprite = component.Owner.GetComponent<ISpriteComponent>();

            var state = component.GetData<string>(CrayonVisuals.State);
            var color = component.GetData<Color>(CrayonVisuals.Color);

            sprite.LayerSetState(0, state);
            sprite.LayerSetColor(0, color);
        }
    }
}
