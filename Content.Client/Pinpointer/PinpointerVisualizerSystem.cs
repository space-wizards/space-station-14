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
            if (!AppearanceSystem.TryGetData<bool>(uid, PinpointerVisuals.IsActive, out var isActive, args.Component) || !isActive)
            {
                sprite.LayerSetVisible(PinpointerLayers.Screen, false);
                return;
            }

            sprite.LayerSetVisible(PinpointerLayers.Screen, true);

            // check distance and direction to target
            if (!AppearanceSystem.TryGetData<Distance>(uid, PinpointerVisuals.TargetDistance, out var dis, args.Component) ||
                !AppearanceSystem.TryGetData<Angle>(uid, PinpointerVisuals.ArrowAngle, out var angle, args.Component))
            {
                sprite.LayerSetState(PinpointerLayers.Screen, "pinonnull");
                sprite.LayerSetRotation(PinpointerLayers.Screen, Angle.Zero);
                return;
            }

            switch (dis)
            {
                case Distance.Reached:
                    sprite.LayerSetState(PinpointerLayers.Screen, "pinondirect");
                    sprite.LayerSetRotation(PinpointerLayers.Screen, Angle.Zero);
                    break;
                case Distance.Close:
                    sprite.LayerSetState(PinpointerLayers.Screen, "pinonclose");
                    sprite.LayerSetRotation(PinpointerLayers.Screen, angle);
                    break;
                case Distance.Medium:
                    sprite.LayerSetState(PinpointerLayers.Screen, "pinonmedium");
                    sprite.LayerSetRotation(PinpointerLayers.Screen, angle);
                    break;
                case Distance.Far:
                    sprite.LayerSetState(PinpointerLayers.Screen, "pinonfar");
                    sprite.LayerSetRotation(PinpointerLayers.Screen, angle);
                    break;
                case Distance.Unknown:
                    sprite.LayerSetState(PinpointerLayers.Screen, "pinonnull");
                    sprite.LayerSetRotation(PinpointerLayers.Screen, Angle.Zero);
                    break;
            }
        }
    }
}
