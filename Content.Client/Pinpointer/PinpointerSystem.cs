using Content.Shared.Pinpointer;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client.Pinpointer;

public sealed class PinpointerSystem : SharedPinpointerSystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;

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
            UpdateArrowAngle(uid, pinpointer, appearance);
        }
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
        Appearance.SetData(uid, PinpointerVisuals.ArrowAngle, angle, appearance);
    }

    private void OnAppearanceChange(EntityUid uid, PinpointerComponent component, ref AppearanceChangeEvent args)
    {
        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        if (!Appearance.TryGetData<Distance>(uid, PinpointerVisuals.TargetDistance, out var distance, args.Component) ||
            !Appearance.TryGetData<Angle>(uid, PinpointerVisuals.ArrowAngle, out var angle, args.Component))
        {
            sprite.LayerSetRotation(PinpointerLayers.Screen, Angle.Zero);
            return;
        }

        switch (distance)
        {
            case Distance.Close:
            case Distance.Medium:
            case Distance.Far:
                sprite.LayerSetRotation(PinpointerLayers.Screen, angle);
                break;
            default:
                sprite.LayerSetRotation(PinpointerLayers.Screen, Angle.Zero);
                break;
        }
    }
}
