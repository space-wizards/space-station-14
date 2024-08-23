using System.Numerics;
using Content.Shared.Camera;
using Content.Shared.Inventory;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.Map;
using Robust.Client.Player;

namespace Content.Client.Movement.Systems;

public partial class EyeCursorOffsetSystem : EntitySystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedContentEyeSystem _contentEye = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IClyde _clyde = default!;

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
    /*public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var eyeEntities = AllEntityQuery<EyeCursorOffsetComponent>();
        while (eyeEntities.MoveNext(out EntityUid entity, out EyeCursorOffsetComponent? eyeCursorOffsetComp))
        {
            OffsetAfterMouse(entity, eyeCursorOffsetComp);
        }
    }*/

    public Vector2? OffsetAfterMouse(EntityUid uid, EyeCursorOffsetComponent? component)
    {
        var localPlayer = _player.LocalPlayer?.ControlledEntity;
        var mousePos = _inputManager.MouseScreenPosition;
        var screenSize = _clyde.MainWindow.Size;
        var minValue = MathF.Min(screenSize.X / 2, screenSize.Y / 2);
        var screenSizeMiddle = new Vector2(-(mousePos.X - screenSize.X / 2) / minValue, (mousePos.Y - screenSize.Y / 2) / minValue);


        if (localPlayer != null)
        {
            var playerPos = _transform.GetWorldPosition(localPlayer.Value);
            
            //var mouseMapPos = _eyeManager.ScreenToMap(new Vector2(mousePos.X, mousePos.Y));

            //Vector2 mouseRelativePos;

            if (component == null)
            {
                component = EnsureComp<EyeCursorOffsetComponent>(uid);
            }

            // Doesn't move the offset if the mouse has left the game window
            if (mousePos.Window != WindowId.Invalid)
            {
                //mouseRelativePos = mouseMapPos.Position - playerPos;
                //Logger.Debug(mouseMapPos.Position.Length().ToString());
                
                var eyeRotation = _eyeManager.CurrentEye.Rotation;
                var mouseRelativePos = Vector2.Transform(screenSizeMiddle, System.Numerics.Quaternion.CreateFromAxisAngle(-System.Numerics.Vector3.UnitZ, (float) (eyeRotation.Opposite().Theta)));
                Logger.Debug(screenSize.ToString());
                Logger.Debug(minValue.ToString());
                Logger.Debug(eyeRotation.ToString());
                Logger.Debug(mousePos.ToString());
                Logger.Debug(mouseRelativePos.ToString());
                
                var modifier = 3f;
                mouseRelativePos *= modifier;
                if (mouseRelativePos.Length() > 3)
                {
                    mouseRelativePos = mouseRelativePos.Normalized() * 3;
                }

                component.TargetPosition = mouseRelativePos;

                //Makes the view not jump immediately when moving the cursor fast
                if (component.CurrentPosition != component.TargetPosition)
                {
                    Vector2 vectorOffset = component.TargetPosition - component.CurrentPosition;
                    if (vectorOffset.Length() > 0.5f)
                    {
                        vectorOffset = vectorOffset.Normalized() * 0.5f;
                    }
                    component.CurrentPosition += vectorOffset;
                }
            }
            return component.CurrentPosition;
        }
        return null;
    }
}
