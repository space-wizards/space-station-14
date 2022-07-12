using System;
using Content.Shared.Movement.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Physics;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client.Eye;

public sealed class EyeLerpingSystem : EntitySystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    // How fast the camera rotates in radians / s
    private const float CameraRotateSpeed = MathF.PI;

    // Safety override
    private const float LerpTimeMax = 1.5f;

    // Lerping information for the player's active eye.
    private readonly EyeLerpInformation _playerActiveEye = new();

    // Eyes other than the primary eye that are currently active.
    private readonly Dictionary<EntityUid, EyeLerpInformation> _activeEyes = new();
    private readonly List<EntityUid> _toRemove = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EyeComponent, ComponentShutdown>(OnEyeShutdown);

        UpdatesAfter.Add(typeof(TransformSystem));
        UpdatesAfter.Add(typeof(PhysicsSystem));
        UpdatesBefore.Add(typeof(EyeUpdateSystem));
    }

    private void OnEyeShutdown(EntityUid uid, EyeComponent component, ComponentShutdown args)
    {
        RemoveEye(uid);
    }

    public void AddEye(EntityUid uid)
    {
        if (!_activeEyes.ContainsKey(uid))
        {
            _activeEyes.Add(uid, new());
        }
    }

    public void RemoveEye(EntityUid uid)
    {
        if (_activeEyes.ContainsKey(uid))
        {
            _activeEyes.Remove(uid);
        }
    }

    public override void FrameUpdate(float frameTime)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        // Always do this one.
        LerpPlayerEye(frameTime);

        foreach (var (entity, info) in _activeEyes)
        {
            LerpEntityEye(entity, info, frameTime);
        }

        if (_toRemove.Count != 0)
        {
            foreach (var entity in _toRemove)
            {
                RemoveEye(entity);
            }

            _toRemove.Clear();
        }
    }

    private void LerpPlayerEye(float frameTime)
    {
        if (_playerManager.LocalPlayer?.ControlledEntity is not {} mob || Deleted(mob))
            return;

        // We can't lerp if the mob can't move!
        if (!TryComp(mob, out InputMoverComponent? mover))
            return;

        LerpEye(_eyeManager.CurrentEye, frameTime, mover.LastGridAngle, _playerActiveEye);
    }

    private void LerpEntityEye(EntityUid uid, EyeLerpInformation info, float frameTime)
    {
        if (!TryComp(uid, out TransformComponent? transform)
            || !TryComp(uid, out EyeComponent? eye)
            || eye.Eye == null
            || !_mapManager.TryGetGrid(transform.GridUid, out var grid))
        {
            _toRemove.Add(uid);
            return;
        }

        LerpEye(eye.Eye, frameTime, grid.WorldRotation, info);
    }

    private void LerpEye(IEye eye, float frameTime, Angle lastAngle, EyeLerpInformation lerpInfo)
    {

        // Let's not turn the camera into a washing machine when the game starts.
        if (lerpInfo.LastGridAngle == null)
        {
            lerpInfo.LastGridAngle = lastAngle;
            eye.Rotation = -lastAngle;
            return;
        }

        // Check if the last lerp grid angle we have is not the same as the last mover grid angle...
        if (!lerpInfo.LastGridAngle.Value.EqualsApprox(lastAngle))
        {
            // And now, we start lerping.
            lerpInfo.LerpTo = lastAngle;
            lerpInfo.LastGridAngle = lastAngle;
            lerpInfo.LerpStartRotation = eye.Rotation;
            lerpInfo.Accumulator = 0f;
        }

        if (lerpInfo.LerpTo != null)
        {
            lerpInfo.Accumulator += frameTime;

            var lerpRot = -lerpInfo.LerpTo.Value.FlipPositive().Reduced();
            var startRot = lerpInfo.LerpStartRotation.FlipPositive().Reduced();

            var changeNeeded = Angle.ShortestDistance(startRot, lerpRot);

            if (changeNeeded.EqualsApprox(Angle.Zero))
            {
                // Nothing to do here!
                lerpInfo.Cleanup(eye);
                return;
            }

            // Get how much the camera should have moved by now. Make it faster depending on the change needed.
            var changeRot = (CameraRotateSpeed * Math.Max(1f, Math.Abs(changeNeeded) * 0.75f)) * lerpInfo.Accumulator * Math.Sign(changeNeeded);

            // How close is this from reaching the end?
            var percentage = (float)Math.Abs(changeRot / changeNeeded);

            eye.Rotation = Angle.Lerp(startRot, lerpRot, percentage);

            // Either we have overshot, or we have taken way too long on this, emergency reset time
            if (percentage >= 1.0f || lerpInfo.Accumulator >= LerpTimeMax)
            {
                lerpInfo.Cleanup(eye);
            }
        }
        else
        {
            // This makes it so rotating the camera manually is impossible...
            // However, it is needed. Why? Because of a funny (hilarious, even) race condition involving
            // ghosting, this system listening for attached mob changes, and the eye rotation being reset after our
            // changes back to zero because of an EyeComponent state coming from the server being applied.
            // At some point we'll need to come up with a solution for that. But for now, I just want to fix this.
            eye.Rotation = -lastAngle;
        }
    }

    private sealed class EyeLerpInformation
    {
        public Angle? LastGridAngle { get; set; }
        public Angle? LerpTo { get; set; }
        public Angle LerpStartRotation { get; set; }
        public float Accumulator { get; set; }

        public void Cleanup(IEye eye)
        {
            eye.Rotation = -LerpTo ?? Angle.Zero;
            LerpStartRotation = eye.Rotation;
            LerpTo = null;
            Accumulator = 0;
        }
    }
}
