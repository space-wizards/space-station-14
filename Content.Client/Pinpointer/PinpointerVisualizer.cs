using Content.Shared.Pinpointer;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Client.Pinpointer
{
    [UsedImplicitly]
    public sealed class PinpointerVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var entities = IoCManager.Resolve<IEntityManager>();
            if (!entities.TryGetComponent(component.Owner, out SpriteComponent sprite))
                return;

            // check if pinpointer screen is active
            if (!component.TryGetData(PinpointerVisuals.IsActive, out bool isActive) || !isActive)
            {
                sprite.LayerSetVisible(PinpointerLayers.Screen, false);
                return;
            }

            // check if it has direction to target
            sprite.LayerSetVisible(PinpointerLayers.Screen, true);
            sprite.LayerSetRotation(PinpointerLayers.Screen, Angle.Zero);

            if (!component.TryGetData(PinpointerVisuals.TargetDirection, out Direction dir) || dir == Direction.Invalid)
            {
                sprite.LayerSetState(PinpointerLayers.Screen, "pinonnull");
                return;
            }

            // check distance to target
            if (!component.TryGetData(PinpointerVisuals.TargetDistance, out Distance dis))
                dis = Distance.UNKNOWN;

            switch (dis)
            {
                case Distance.REACHED:
                    sprite.LayerSetState(PinpointerLayers.Screen, "pinondirect");
                    break;
                case Distance.CLOSE:
                    sprite.LayerSetState(PinpointerLayers.Screen, "pinonclose");
                    sprite.LayerSetRotation(PinpointerLayers.Screen, dir.ToAngle());
                    break;
                case Distance.MEDIUM:
                    sprite.LayerSetState(PinpointerLayers.Screen, "pinonmedium");
                    sprite.LayerSetRotation(PinpointerLayers.Screen, dir.ToAngle());
                    break;
                case Distance.FAR:
                case Distance.UNKNOWN:
                    sprite.LayerSetState(PinpointerLayers.Screen, "pinonfar");
                    sprite.LayerSetRotation(PinpointerLayers.Screen, dir.ToAngle());
                    break;
            }
        }
    }
}
