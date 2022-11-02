using Content.Shared.Pinpointer;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Pinpointer
{
    [UsedImplicitly]
    public sealed class PinpointerVisualizerSystem : VisualizerSystem<PinpointerComponent>
    {
        protected override void OnAppearanceChange(EntityUid uid, PinpointerComponent component, ref AppearanceChangeEvent args)
        {
            base.OnAppearanceChange(uid, component, ref args);

            if (!TryComp(component.Owner, out SpriteComponent? sprite))
                return;

            // check if pinpointer screen is active
            if (!args.Component.TryGetData(PinpointerVisuals.IsActive, out bool isActive) || !isActive)
            {
                sprite.LayerSetVisible(PinpointerLayers.Screen, false);
                return;
            }

            // check if it has direction to target
            sprite.LayerSetVisible(PinpointerLayers.Screen, true);
            sprite.LayerSetRotation(PinpointerLayers.Screen, Angle.Zero);

            if (!args.Component.TryGetData(PinpointerVisuals.TargetDirection, out Direction dir) || dir == Direction.Invalid)
            {
                sprite.LayerSetState(PinpointerLayers.Screen, "pinonnull");
                return;
            }

            // check distance to target
            if (!args.Component.TryGetData(PinpointerVisuals.TargetDistance, out Distance dis))
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
