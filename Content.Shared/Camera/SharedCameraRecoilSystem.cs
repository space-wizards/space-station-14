using JetBrains.Annotations;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Shared.Camera;

[UsedImplicitly]
public abstract class SharedCameraRecoilSystem : EntitySystem
{
    /// <summary>
    ///     Maximum rate of magnitude restore towards 0 kick.
    /// </summary>
    private const float RestoreRateMax = 30f;

    /// <summary>
    ///     Minimum rate of magnitude restore towards 0 kick.
    /// </summary>
    private const float RestoreRateMin = 0.1f;

    /// <summary>
    ///     Time in seconds since the last kick that lerps RestoreRateMin and RestoreRateMax
    /// </summary>
    private const float RestoreRateRamp = 4f;

    /// <summary>
    ///     The maximum magnitude of the kick applied to the camera at any point.
    /// </summary>
    protected const float KickMagnitudeMax = 1f;

    private ISawmill _log = default!;

    public override void Initialize()
    {
        base.Initialize();
        _log = Logger.GetSawmill($"ecs.systems.{nameof(SharedCameraRecoilSystem)}");
    }

    /// <summary>
    ///     Applies explosion/recoil/etc kickback to the view of the entity.
    /// </summary>
    /// <remarks>
    ///     If the entity is missing <see cref="CameraRecoilComponent" /> and/or <see cref="SharedEyeComponent" />,
    ///     this call will have no effect. It is safe to call this function on any entity.
    /// </remarks>
    public abstract void KickCamera(EntityUid euid, Vector2 kickback, CameraRecoilComponent? component = null);

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
