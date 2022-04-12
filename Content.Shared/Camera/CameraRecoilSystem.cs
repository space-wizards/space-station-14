using System;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Shared.Camera;

[UsedImplicitly]
public sealed class CameraRecoilSystem : EntitySystem
{
    /// <summary>
    ///     Maximum rate of magnitude restore towards 0 kick.
    /// </summary>
    private const float RestoreRateMax = 15f;

    /// <summary>
    ///     Minimum rate of magnitude restore towards 0 kick.
    /// </summary>
    private const float RestoreRateMin = 1f;

    /// <summary>
    ///     Time in seconds since the last kick that lerps RestoreRateMin and RestoreRateMax
    /// </summary>
    private const float RestoreRateRamp = 0.1f;

    /// <summary>
    ///     The maximum magnitude of the kick applied to the camera at any point.
    /// </summary>
    private const float KickMagnitudeMax = 2f;

    private readonly ISawmill _log;

    private CameraRecoilSystem(IEntityManager entityManager)
    : base(entityManager)
    {
        _log = Logger.GetSawmill($"ecs.systems.{nameof(CameraRecoilSystem)}");

        SubscribeNetworkEvent<CameraKickEvent>(HandleCameraKick);
    }

    /// <summary>
    ///     Applies explosion/recoil/etc kickback to the view of the entity.
    /// </summary>
    /// <remarks>
    ///     If the entity is missing <see cref="CameraRecoilComponent" /> and/or <see cref="SharedEyeComponent" />,
    ///     this call will have no effect. It is safe to call this function on any entity.
    /// </remarks>
    /// <param name="euid">Entity to apply the kickback to.</param>
    /// <param name="kickback">The amount of kick to offset the view of the entity. World coordinates, in meters.</param>
    public void KickCamera(EntityUid euid, Vector2 kickback)
    {
        if (!EntityManager.HasComponent<CameraRecoilComponent>(euid))
            return;

        //TODO: This should only be sent to clients registered as viewers to the entity.
        RaiseNetworkEvent(new CameraKickEvent(euid, kickback), Filter.Broadcast());
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        foreach (var entity in EntityManager.EntityQuery<SharedEyeComponent, CameraRecoilComponent>(true))
        {
            var recoil = entity.Item2;
            var eye = entity.Item1;
            var magnitude = recoil.CurrentKick.Length;
            if (magnitude <= 0.005f)
            {
                recoil.CurrentKick = Vector2.Zero;
                eye.Offset = recoil.BaseOffset + recoil.CurrentKick;
            }
            else // Continually restore camera to 0.
            {
                var normalized = recoil.CurrentKick.Normalized;
                recoil.LastKickTime += frameTime;
                var restoreRate = MathHelper.Lerp(RestoreRateMin, RestoreRateMax, Math.Min(1, recoil.LastKickTime / RestoreRateRamp));
                var restore = normalized * restoreRate * frameTime;
                var (x, y) = recoil.CurrentKick - restore;
                if (Math.Sign(x) != Math.Sign(recoil.CurrentKick.X)) x = 0;

                if (Math.Sign(y) != Math.Sign(recoil.CurrentKick.Y)) y = 0;

                recoil.CurrentKick = (x, y);

                eye.Offset = recoil.BaseOffset + recoil.CurrentKick;
            }
        }
    }

    private void HandleCameraKick(CameraKickEvent args)
    {
        if (!EntityManager.TryGetComponent(args.Euid, out CameraRecoilComponent recoil))
        {
            _log.Warning($"Received a kick for euid {args.Euid}, but it is missing required components.");
            return;
        }

        if (!float.IsFinite(args.Recoil.X) || !float.IsFinite(args.Recoil.Y))
        {
            _log.Error($"CameraRecoilComponent on entity {recoil.Owner} passed a NaN recoil value. Ignoring.");
            return;
        }

        // Use really bad math to "dampen" kicks when we're already kicked.
        var existing = recoil.CurrentKick.Length;
        var dampen = existing / KickMagnitudeMax;
        recoil.CurrentKick += args.Recoil * (1 - dampen);

        if (recoil.CurrentKick.Length > KickMagnitudeMax)
            recoil.CurrentKick = recoil.CurrentKick.Normalized * KickMagnitudeMax;

        recoil.LastKickTime = 0;
    }
}

[Serializable]
[NetSerializable]
public sealed class CameraKickEvent : EntityEventArgs
{
    public readonly EntityUid Euid;
    public readonly Vector2 Recoil;

    public CameraKickEvent(EntityUid euid, Vector2 recoil)
    {
        Recoil = recoil;
        Euid = euid;
    }
}
