using System.Numerics;
using Content.Client.Movement.Components;
using Content.Client.Viewport;
using Content.Shared.Camera;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.Map;

namespace Content.Client.Movement.Systems;

public sealed partial class EyeCursorOffsetSystem : EntitySystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;

    // This value is here to make sure the user doesn't have to move their mouse
    // all the way out to the edge of the screen to get the full offset.
    private static float _edgeOffset = 0.8f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EyeCursorOffsetComponent, GetEyeOffsetEvent>(OnGetEyeOffsetEvent);
    }

    private void OnGetEyeOffsetEvent(EntityUid uid, EyeCursorOffsetComponent component, ref GetEyeOffsetEvent args)
    {
        var offset = OffsetAfterMouse(uid, component);
        if (offset == null)
            return;

        args.Offset += offset.Value;
    }

    public Vector2? OffsetAfterMouse(EntityUid uid, EyeCursorOffsetComponent? component)
    {
        // We need the main viewport where the game content is displayed, as certain UI layouts (e.g. Separated HUD) can make it a different size to the game window.
        if (_eyeManager.MainViewport is not ScalingViewport vp)
            return null;

        var mousePos = _inputManager.MouseScreenPosition.Position; // TODO: If we ever get a right-aligned Separated HUD setting, this might need to be adjusted for that.

        var viewportSize = vp.PixelSize; // The size of the game viewport, including black bars - does not include the chatbox in Separated HUD view.
        var scalingViewportSize = vp.ViewportSize * vp.CurrentRenderScale; // The size of the viewport in which the game is rendered (i.e. not including black bars). Note! Can extend outside the game window with certain zoom settings!
        var visibleViewportSize = Vector2.Min(viewportSize, scalingViewportSize); // The size of the game viewport that is "actually visible" to the player, cutting off over-extensions and not counting black bar padding.

        Matrix3x2.Invert(_eyeManager.MainViewport.GetLocalToScreenMatrix(), out var matrix);
        var mouseCoords = Vector2.Transform(mousePos, matrix); // Gives the mouse position inside of the *scaling viewport*, i.e. 0,0 is inside the black bars. Note! 0,0 can be outside the game window with certain zoom settings!

        var boundedMousePos = Vector2.Clamp(Vector2.Min(mouseCoords, mousePos), Vector2.Zero, visibleViewportSize); // Mouse position inside the visible game viewport's bounds.

        var offsetRadius = MathF.Min(visibleViewportSize.X / 2f, visibleViewportSize.Y / 2f) * _edgeOffset;
        var mouseNormalizedPos = new Vector2(-(boundedMousePos.X - visibleViewportSize.X / 2f) / offsetRadius, (boundedMousePos.Y - visibleViewportSize.Y / 2f) / offsetRadius);

        if (component == null)
            component = EnsureComp<EyeCursorOffsetComponent>(uid);

        // Doesn't move the offset if the mouse has left the game window!
        if (_inputManager.MouseScreenPosition.Window != WindowId.Invalid)
        {
            // The offset must account for the in-world rotation.
            var eyeRotation = _eyeManager.CurrentEye.Rotation;
            var mouseActualRelativePos = Vector2.Transform(mouseNormalizedPos, System.Numerics.Quaternion.CreateFromAxisAngle(-System.Numerics.Vector3.UnitZ, (float)(eyeRotation.Opposite().Theta))); // I don't know, it just works.

            // Caps the offset into a circle around the player.
            mouseActualRelativePos *= component.MaxOffset;
            if (mouseActualRelativePos.Length() > component.MaxOffset)
            {
                mouseActualRelativePos = mouseActualRelativePos.Normalized() * component.MaxOffset;
            }

            component.TargetPosition = mouseActualRelativePos;

            //Makes the view not jump immediately when moving the cursor fast.
            if (component.CurrentPosition != component.TargetPosition)
            {
                Vector2 vectorOffset = component.TargetPosition - component.CurrentPosition;
                if (vectorOffset.Length() > component.OffsetSpeed)
                {
                    vectorOffset = vectorOffset.Normalized() * component.OffsetSpeed; // TODO: Probably needs to properly account for time delta or something.
                }
                component.CurrentPosition += vectorOffset;
            }
        }
        return component.CurrentPosition;
    }
}
