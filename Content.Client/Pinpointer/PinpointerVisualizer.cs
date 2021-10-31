using Content.Shared.Pinpointer;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Client.Pinpointer
{
    [UsedImplicitly]
    public class PinpointerVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent<SpriteComponent>(out var sprite))
                return;

            // check if pinpointer screen is active
            if (!component.TryGetData(PinpointerVisuals.IsActive, out bool isActive) || !isActive)
            {
                sprite.LayerSetVisible(PinpointerLayers.Screen, false);
                return;
            }

            // check if it has direction to target
            sprite.LayerSetVisible(PinpointerLayers.Screen, true);

            var dir = GetDirection(component);
            if (dir == Direction.Invalid)
            {
                sprite.LayerSetState(PinpointerLayers.Screen, "pinonnull");
                return;
            }

            var eye = IoCManager.Resolve<IEyeManager>().CurrentEye;

            sprite.LayerSetState(PinpointerLayers.Screen, "pinonfar");
            sprite.LayerSetRotation(PinpointerLayers.Screen, dir.ToAngle() + eye.Rotation);
        }

        private Direction GetDirection(AppearanceComponent component)
        {
            if (!component.TryGetData(PinpointerVisuals.TargetDirection, out sbyte dirByte))
                return Direction.Invalid;

            return (Direction) dirByte;
        }
    }

    public enum PinpointerLayers : byte
    {
        Base,
        Screen
    }
}
