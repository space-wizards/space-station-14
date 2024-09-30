using Content.Shared.MouseRotator;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.MouseRotator;

/// <inheritdoc/>
public sealed class MouseRotatorSystem : SharedMouseRotatorSystem
{
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted || !_input.MouseScreenPosition.IsValid)
            return;

        var player = _player.LocalEntity;

        if (player == null || !TryComp<MouseRotatorComponent>(player, out var rotator))
            return;

        var xform = Transform(player.Value);

        // Get mouse loc and convert to angle based on player location
        var coords = _input.MouseScreenPosition;
        var mapPos = _eye.PixelToMap(coords);

        if (mapPos.MapId == MapId.Nullspace)
            return;

        var angle = (mapPos.Position - _transform.GetMapCoordinates(player.Value, xform: xform).Position).ToWorldAngle();

        var curRot = _transform.GetWorldRotation(xform);

        // 4-dir handling is separate --
        // only raise event if the cardinal direction has changed
        if (rotator.Simple4DirMode)
        {
            var eyeRot = _eye.CurrentEye.Rotation; // camera rotation
            var angleDir = (angle + eyeRot).GetCardinalDir(); // apply GetCardinalDir in the camera frame, not in the world frame
            if (angleDir == (curRot + eyeRot).GetCardinalDir())
                return;

            var rotation = angleDir.ToAngle() - eyeRot; // convert back to world frame
            if (rotation >= Math.PI) // convert to [-PI, +PI)
                rotation -= 2 * Math.PI;
            else if (rotation < -Math.PI)
                rotation += 2 * Math.PI;
            RaisePredictiveEvent(new RequestMouseRotatorRotationEvent
            {
                Rotation = rotation
            });

            return;
        }

        // Don't raise event if mouse ~hasn't moved (or if too close to goal rotation already)
        var diff = Angle.ShortestDistance(angle, curRot);
        if (Math.Abs(diff.Theta) < rotator.AngleTolerance.Theta)
            return;

        if (rotator.GoalRotation != null)
        {
            var goalDiff = Angle.ShortestDistance(angle, rotator.GoalRotation.Value);
            if (Math.Abs(goalDiff.Theta) < rotator.AngleTolerance.Theta)
                return;
        }

        RaisePredictiveEvent(new RequestMouseRotatorRotationEvent
        {
            Rotation = angle
        });
    }
}
