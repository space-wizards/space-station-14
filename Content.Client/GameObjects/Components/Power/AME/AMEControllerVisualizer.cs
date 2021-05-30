using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using static Content.Shared.GameObjects.Components.Power.AME.SharedAMEControllerComponent;

namespace Content.Client.GameObjects.Components.Power.AME
{
    [UsedImplicitly]
    public class AMEControllerVisualizer : AppearanceVisualizer
    {
        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);
            var sprite = entity.GetComponent<ISpriteComponent>();

            sprite.LayerMapSet(Layers.Display, sprite.AddLayerState("control_on"));
            sprite.LayerSetVisible(Layers.Display, false);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            var sprite = component.Owner.GetComponent<ISpriteComponent>();
            if (component.TryGetData<string>(AMEControllerVisuals.DisplayState, out var state))
            {
                switch (state)
                {
                    case "on":
                        sprite.LayerSetState(Layers.Display, "control_on");
                        sprite.LayerSetVisible(Layers.Display, true);
                        break;
                    case "critical":
                        sprite.LayerSetState(Layers.Display, "control_critical");
                        sprite.LayerSetVisible(Layers.Display, true);
                        break;
                    case "fuck":
                        sprite.LayerSetState(Layers.Display, "control_fuck");
                        sprite.LayerSetVisible(Layers.Display, true);
                        break;
                    case "off":
                        sprite.LayerSetVisible(Layers.Display, false);
                        break;
                    default:
                        sprite.LayerSetVisible(Layers.Display, false);
                        break;
                }
            }
        }

        enum Layers : byte
        {
            Display,
        }
    }
}
