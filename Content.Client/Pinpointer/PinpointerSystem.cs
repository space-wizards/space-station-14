using Content.Shared.Pinpointer;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client.Pinpointer;

public sealed class PinpointerSystem : SharedPinpointerSystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PinpointerComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // we want to show pinpointers arrow direction relative
        // to players eye rotation (like it was in SS13)

        // because eye can change it rotation anytime
        // we need to update this arrow in a update loop
        var query = EntityQueryEnumerator<PinpointerComponent, AppearanceComponent>();
        while (query.MoveNext(out var uid, out var pinpointer, out var appearance))
        {
            UpdateAppearance(uid, pinpointer, appearance);
            UpdateArrowAngle(uid, pinpointer, appearance);
        }
    }

    private void UpdateAppearance(EntityUid uid, PinpointerComponent pinpointer, AppearanceComponent appearance)
    {
        _appearance.SetData(uid, PinpointerVisuals.IsActive, pinpointer.IsActive, appearance);
        _appearance.SetData(uid, PinpointerVisuals.TargetDistance, pinpointer.DistanceToTarget, appearance);
    }

    /// <summary>
    ///     Transform pinpointer arrow from world space to eye space
    ///     And send it to the appearance component
    /// </summary>
    private void UpdateArrowAngle(EntityUid uid, PinpointerComponent pinpointer, AppearanceComponent appearance)
    {
        if (!pinpointer.HasTarget)
            return;
        var eye = _eyeManager.CurrentEye;
        var angle = pinpointer.ArrowAngle + eye.Rotation;
        _appearance.SetData(uid, PinpointerVisuals.ArrowAngle, angle, appearance);
    }

    private void OnAppearanceChange(EntityUid uid, PinpointerComponent component, ref AppearanceChangeEvent args)
    {
        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        // check if pinpointer screen is active
        if (!_appearance.TryGetData<bool>(uid, PinpointerVisuals.IsActive, out var isActive, args.Component) || !isActive)
        {
            sprite.LayerSetVisible(PinpointerLayers.Screen, false);
            return;
        }

        sprite.LayerSetVisible(PinpointerLayers.Screen, true);

        // check distance and direction to target
        if (!_appearance.TryGetData<Distance>(uid, PinpointerVisuals.TargetDistance, out var dis, args.Component) ||
            !_appearance.TryGetData<Angle>(uid, PinpointerVisuals.ArrowAngle, out var angle, args.Component))
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
