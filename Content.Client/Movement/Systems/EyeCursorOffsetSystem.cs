using System.Numerics;
using Content.Client.Movement.Components;
using Content.Client.UserInterface.Controls;
using Content.Shared.Camera;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.Map;
using Robust.Client.Player;
using Robust.Client.UserInterface;

namespace Content.Client.Movement.Systems;

public partial class EyeCursorOffsetSystem : EntitySystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    // This value is here to make sure the user doesn't have to move their mouse
    // all the way out to the edge of the screen to get the full offset.
    static private float _edgeOffset = 0.9f;

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
        // We need the main viewport where the game content is displayed, as certain UI layouts (e.g. Separated Chat)
        // can make it a different size to the game window.
        if (_uiManager.ActiveScreen == null ||!_uiManager.ActiveScreen!.TryGetWidget<MainViewport>(out var mainViewport))
            return null;

        var localPlayer = _player.LocalPlayer?.ControlledEntity;
        var mousePos = _inputManager.MouseScreenPosition;
        var screenSize = mainViewport.Size;

        // Defines the circle at which the offset is capped.
        var minValue = MathF.Min(screenSize.X / 2, screenSize.Y / 2) * _edgeOffset;

        var mouseNormalizedPos = new Vector2(-(mousePos.X - screenSize.X / 2) / minValue, (mousePos.Y - screenSize.Y / 2) / minValue); // X needs to be inverted here for some reason, otherwise it ends up flipped.

        if (localPlayer == null)
            return null;

        var playerPos = _transform.GetWorldPosition(localPlayer.Value);

        if (component == null)
        {
            component = EnsureComp<EyeCursorOffsetComponent>(uid);
        }

        // Doesn't move the offset if the mouse has left the game window!
        if (mousePos.Window != WindowId.Invalid)
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
                    vectorOffset = vectorOffset.Normalized() * component.OffsetSpeed;
                }
                component.CurrentPosition += vectorOffset;
            }
        }
        return component.CurrentPosition;
    }
}
