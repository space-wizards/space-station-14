using System;
using Content.Shared.Movement.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Physics;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client.Eye;

public class EyeLerpingSystem : EntitySystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private Angle? _lastGridAngle;
    private Angle? _lerpTo;
    private Angle _lerpStartRotation;
    private float _accumulator;

    // How fast the camera rotates in radians / s
    private const float CameraRotateSpeed = MathF.PI;

    // Safety override
    private const float LerpTimeMax = 1.5f;


    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(TransformSystem));
        UpdatesAfter.Add(typeof(PhysicsSystem));
        UpdatesBefore.Add(typeof(EyeUpdateSystem));
    }

    public override void FrameUpdate(float frameTime)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        var currentEye = _eyeManager.CurrentEye;

        if (_playerManager.LocalPlayer?.ControlledEntity is not {} mob || Deleted(mob))
            return;

        // We can't lerp if the mob can't move!
        if (!TryComp(mob, out IMoverComponent? mover))
            return;

        var moverLastGridAngle = mover.LastGridAngle;

        // Let's not turn the camera into a washing machine when the game starts.
        if (_lastGridAngle == null)
        {
            _lastGridAngle = moverLastGridAngle;
            currentEye.Rotation = -moverLastGridAngle;
            return;
        }

        // Check if the last lerp grid angle we have is not the same as the last mover grid angle...
        if (!_lastGridAngle.Value.EqualsApprox(moverLastGridAngle))
        {
            // And now, we start lerping.
            _lerpTo = moverLastGridAngle;
            _lastGridAngle = moverLastGridAngle;
            _lerpStartRotation = currentEye.Rotation;
            _accumulator = 0f;
        }

        if (_lerpTo != null)
        {
            _accumulator += frameTime;

            var lerpRot = -_lerpTo.Value.FlipPositive().Reduced();
            var startRot = _lerpStartRotation.FlipPositive().Reduced();

            var changeNeeded = Angle.ShortestDistance(startRot, lerpRot);

            if (changeNeeded.EqualsApprox(Angle.Zero))
            {
                // Nothing to do here!
                CleanupLerp();
                return;
            }

            // Get how much the camera should have moved by now. Make it faster depending on the change needed.
            var changeRot = (CameraRotateSpeed * Math.Max(1f, Math.Abs(changeNeeded) * 0.75f)) * _accumulator * Math.Sign(changeNeeded);

            // How close is this from reaching the end?
            var percentage = (float)Math.Abs(changeRot / changeNeeded);

            currentEye.Rotation = Angle.Lerp(startRot, lerpRot, percentage);

            // Either we have overshot, or we have taken way too long on this, emergency reset time
            if (percentage >= 1.0f || _accumulator >= LerpTimeMax)
            {
                CleanupLerp();
            }

            void CleanupLerp()
            {
                currentEye.Rotation = -_lerpTo.Value;
                _lerpStartRotation = currentEye.Rotation;
                _lerpTo = null;
                _accumulator = 0f;
            }
        }
        else
        {
            // This makes it so rotating the camera manually is impossible...
            // However, it is needed. Why? Because of a funny (hilarious, even) race condition involving
            // ghosting, this system listening for attached mob changes, and the eye rotation being reset after our
            // changes back to zero because of an EyeComponent state coming from the server being applied.
            // At some point we'll need to come up with a solution for that. But for now, I just want to fix this.
            currentEye.Rotation = -moverLastGridAngle;
        }
    }
}
