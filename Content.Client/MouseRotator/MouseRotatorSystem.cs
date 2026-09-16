using Content.Shared.MouseRotator;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.Timing;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.MouseRotator;

/// <inheritdoc/>
public sealed partial class MouseRotatorSystem : SharedMouseRotatorSystem
{
    [Dependency] private IInputManager _input = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IClientGameTiming _timing = default!;
    [Dependency] private IEyeManager _eye = default!;
    [Dependency] private TransformSystem _transform = default!;

    private EntityUid _renderRotationOverride;
    private Angle _renderRotation;
    private Angle _simulationRotationAtOverride;
    private GameTick _renderRotationInactiveTick;
    private bool _renderRotationInactive;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesAfter.Add(typeof(EyeSystem));
    }

    public override void Shutdown()
    {
        ClearRenderRotationOverride();
        base.Shutdown();
    }

    public override void Update(float frameTime)
    {
        var player = _player.LocalEntity;
        var inactive = _renderRotationInactive && _timing.LastRealTick < _renderRotationInactiveTick;
        MouseRotatorComponent? rotator = null;
        TransformComponent? xform = null;
        var snapRotation = false;
        var oldWorldRotation = Angle.Zero;

        if (player != null && TryComp(player, out rotator))
        {
            xform = Transform(player.Value);
            snapRotation = rotator.Simple4DirMode;
            if (snapRotation)
                oldWorldRotation = _transform.GetWorldRotation(xform);
        }

        if (!inactive &&
            _timing.IsFirstTimePredicted &&
            _input.MouseScreenPosition.IsValid &&
            player != null &&
            rotator != null &&
            xform != null)
        {
            UpdateMouseRotation(player.Value, rotator, xform);
        }

        base.Update(frameTime);

        if (!inactive &&
            snapRotation &&
            player != null &&
            !oldWorldRotation.EqualsApprox(_transform.GetWorldRotation(player.Value)))
        {
            _transform.SnapRenderRotation(player.Value);
        }
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var player = _player.LocalEntity;
        if (player == null)
        {
            ClearRenderRotationOverride();
            return;
        }

        if (_renderRotationOverride.IsValid() && _renderRotationOverride != player.Value)
            ClearRenderRotationOverride();

        if (!TryComp(player, out MouseRotatorComponent? rotator))
        {
            if (_renderRotationOverride == player.Value && !_renderRotationInactive)
            {
                _renderRotationInactive = true;
                _renderRotationInactiveTick = _timing.CurTick;
            }

            UpdateInactiveRenderRotationOverride(player.Value);
            return;
        }

        if (_renderRotationInactive)
        {
            if (_timing.LastRealTick < _renderRotationInactiveTick)
                return;

            _renderRotationInactive = false;
        }

        if (!rotator.Simple4DirMode)
        {
            ClearRenderRotationOverride();
            return;
        }

        if (!_input.MouseScreenPosition.IsValid)
            return;

        var xform = Transform(player.Value);
        if (!TryGetMouseAngle(player.Value, xform, out var angle))
            return;

        var eyeRotation = _eye.CurrentEye.Rotation;
        var rotation = GetCardinalRotation(angle, eyeRotation);

        _transform.SetRenderRotationOverride(player.Value, rotation);
        _renderRotationOverride = player.Value;
        _renderRotation = rotation;
        _simulationRotationAtOverride = _transform.GetWorldRotation(player.Value);
    }

    private void UpdateMouseRotation(EntityUid player, MouseRotatorComponent rotator, TransformComponent xform)
    {
        if (!TryGetMouseAngle(player, xform, out var angle))
            return;

        var curRot = _transform.GetWorldRotation(xform);

        // 4-dir handling is separate --
        // only raise event if the cardinal direction has changed
        if (rotator.Simple4DirMode)
        {
            var eyeRot = _eye.CurrentEye.Rotation; // camera rotation
            var angleDir = (angle + eyeRot).GetCardinalDir(); // apply GetCardinalDir in the camera frame, not in the world frame
            if (angleDir == (curRot + eyeRot).GetCardinalDir())
                return;

            var rotation = GetCardinalRotation(angle, eyeRot);
            RaisePredictiveEvent(new RequestMouseRotatorRotationEvent
            {
                Rotation = rotation,
                User = GetNetEntity(player)
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
            Rotation = angle,
            User = GetNetEntity(player)
        });
    }

    private bool TryGetMouseAngle(EntityUid player, TransformComponent xform, out Angle angle)
    {
        var mapPos = _eye.PixelToMap(_input.MouseScreenPosition);
        var playerPos = _transform.GetRenderMapCoordinates((player, xform));

        if (mapPos.MapId == MapId.Nullspace || mapPos.MapId != playerPos.MapId)
        {
            angle = default;
            return false;
        }

        angle = (mapPos.Position - playerPos.Position).ToWorldAngle();
        return true;
    }

    private static Angle GetCardinalRotation(Angle angle, Angle eyeRotation)
    {
        var rotation = (angle + eyeRotation).GetCardinalDir().ToAngle() - eyeRotation;
        if (rotation >= Math.PI)
            rotation -= 2 * Math.PI;
        else if (rotation < -Math.PI)
            rotation += 2 * Math.PI;

        return rotation;
    }

    private void ClearRenderRotationOverride()
    {
        if (!_renderRotationOverride.IsValid())
            return;

        _transform.ClearRenderRotationOverride(_renderRotationOverride);
        _renderRotationOverride = EntityUid.Invalid;
        _renderRotationInactive = false;
    }

    private void UpdateInactiveRenderRotationOverride(EntityUid player)
    {
        if (_renderRotationOverride != player)
            return;

        if (_renderRotationInactive && _timing.LastRealTick < _renderRotationInactiveTick)
            return;

        var simulationRotation = _transform.GetWorldRotation(player);
        if (simulationRotation.EqualsApprox(_renderRotation) ||
            !simulationRotation.EqualsApprox(_simulationRotationAtOverride))
        {
            ClearRenderRotationOverride();
        }
    }
}
