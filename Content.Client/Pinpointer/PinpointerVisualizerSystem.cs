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
                dis = Distance.Unknown;

            switch (dis)
            {
                case Distance.Reached:
                    sprite.LayerSetState(PinpointerLayers.Screen, "pinondirect");
                    break;
                case Distance.Close:
                    sprite.LayerSetState(PinpointerLayers.Screen, "pinonclose");
                    sprite.LayerSetRotation(PinpointerLayers.Screen, dir.ToAngle());
                    break;
                case Distance.Medium:
                    sprite.LayerSetState(PinpointerLayers.Screen, "pinonmedium");
                    sprite.LayerSetRotation(PinpointerLayers.Screen, dir.ToAngle());
                    break;
                case Distance.Far:
                case Distance.Unknown:
                    sprite.LayerSetState(PinpointerLayers.Screen, "pinonfar");
                    sprite.LayerSetRotation(PinpointerLayers.Screen, dir.ToAngle());
                    break;
            }
        }
    }
}
