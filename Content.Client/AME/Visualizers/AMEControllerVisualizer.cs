using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using static Content.Shared.AME.SharedAMEControllerComponent;

namespace Content.Client.AME.Visualizers
{
    [UsedImplicitly]
    public class AMEControllerVisualizer : AppearanceVisualizer
    {
        public override void InitializeEntity(EntityUid entity)
        {
            base.InitializeEntity(entity);
            var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<ISpriteComponent>(entity);

            sprite.LayerMapSet(Layers.Display, sprite.AddLayerState("control_on"));
            sprite.LayerSetVisible(Layers.Display, false);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<ISpriteComponent>(component.Owner);
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
