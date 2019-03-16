using System;
using Content.Shared.GameObjects.Components.Doors;
using SS14.Client.GameObjects;
using SS14.Client.Interfaces.GameObjects.Components;

namespace Content.Client.GameObjects.Components.Doors
{
    public class AirlockVisualizer2D : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var sprite = component.Owner.GetComponent<ISpriteComponent>();
            if (!component.TryGetData(DoorVisuals.VisualState, out DoorVisualState state))
            {
                state = DoorVisualState.Closed;
            }

            switch (state)
            {
                case DoorVisualState.Closed:
                case DoorVisualState.Closing:
                    sprite.LayerSetState(DoorVisualLayers.Base, "closed");
                    break;
                case DoorVisualState.Opening:
                case DoorVisualState.Open:
                    sprite.LayerSetState(DoorVisualLayers.Base, "open");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public enum DoorVisualLayers
    {
        Base
    }
}
