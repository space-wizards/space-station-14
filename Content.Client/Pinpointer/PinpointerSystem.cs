using Content.Shared.Pinpointer;
using Robust.Client.Graphics;

namespace Content.Client.Pinpointer;

public sealed class PinpointerSystem : SharedPinpointerSystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

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
}
