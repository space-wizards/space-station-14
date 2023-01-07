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

            if (args.Sprite == null)
            {
                return;
            }

            // check if pinpointer screen is active
            if (!AppearanceSystem.TryGetData<bool>(uid, PinpointerVisuals.IsActive, out var isActive) || !isActive)
            {
                args.Sprite.LayerSetVisible(PinpointerLayers.Screen, false);
                return;
            }

            args.Sprite.LayerSetVisible(PinpointerLayers.Screen, true);

            // check distance and direction to target
            if (!AppearanceSystem.TryGetData<Distance>(uid, PinpointerVisuals.TargetDistance, out var dis) ||
                !AppearanceSystem.TryGetData<Angle>(uid, PinpointerVisuals.ArrowAngle, out var angle))
            {
                args.Sprite.LayerSetState(PinpointerLayers.Screen, "pinonnull");
                args.Sprite.LayerSetRotation(PinpointerLayers.Screen, Angle.Zero);
                return;
            }

            switch (dis)
            {
                case Distance.Reached:
                    args.Sprite.LayerSetState(PinpointerLayers.Screen, "pinondirect");
                    args.Sprite.LayerSetRotation(PinpointerLayers.Screen, Angle.Zero);
                    break;
                case Distance.Close:
                    args.Sprite.LayerSetState(PinpointerLayers.Screen, "pinonclose");
                    args.Sprite.LayerSetRotation(PinpointerLayers.Screen, angle);
                    break;
                case Distance.Medium:
                    args.Sprite.LayerSetState(PinpointerLayers.Screen, "pinonmedium");
                    args.Sprite.LayerSetRotation(PinpointerLayers.Screen, angle);
                    break;
                case Distance.Far:
                    args.Sprite.LayerSetState(PinpointerLayers.Screen, "pinonfar");
                    args.Sprite.LayerSetRotation(PinpointerLayers.Screen, angle);
                    break;
                case Distance.Unknown:
                    args.Sprite.LayerSetState(PinpointerLayers.Screen, "pinonnull");
                    args.Sprite.LayerSetRotation(PinpointerLayers.Screen, Angle.Zero);
                    break;
            }
        }
    }
}
