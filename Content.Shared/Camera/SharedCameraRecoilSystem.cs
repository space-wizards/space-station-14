using System.Numerics;
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

    [Dependency] private readonly SharedEyeSystem _eye = default!;

    /// <summary>
    ///     Applies explosion/recoil/etc kickback to the view of the entity.
    /// </summary>
    /// <remarks>
    ///     If the entity is missing <see cref="CameraRecoilComponent" /> and/or <see cref="EyeComponent" />,
    ///     this call will have no effect. It is safe to call this function on any entity.
    /// </remarks>
    public abstract void KickCamera(EntityUid euid, Vector2 kickback, CameraRecoilComponent? component = null);

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = AllEntityQuery<EyeComponent, CameraRecoilComponent>();

        while (query.MoveNext(out var uid, out var eye, out var recoil))
        {
            var magnitude = recoil.CurrentKick.Length();
            if (magnitude <= 0.005f)
            {
                recoil.CurrentKick = Vector2.Zero;
                _eye.SetOffset(uid, recoil.BaseOffset + recoil.CurrentKick, eye);
            }
            else // Continually restore camera to 0.
            {
                var normalized = recoil.CurrentKick.Normalized();
                recoil.LastKickTime += frameTime;
                var restoreRate = MathHelper.Lerp(RestoreRateMin, RestoreRateMax, Math.Min(1, recoil.LastKickTime / RestoreRateRamp));
                var restore = normalized * restoreRate * frameTime;
                var (x, y) = recoil.CurrentKick - restore;
                if (Math.Sign(x) != Math.Sign(recoil.CurrentKick.X)) x = 0;

                if (Math.Sign(y) != Math.Sign(recoil.CurrentKick.Y)) y = 0;

                recoil.CurrentKick = new Vector2(x, y);

                _eye.SetOffset(uid, recoil.BaseOffset + recoil.CurrentKick, eye);
            }
        }
    }
}

[Serializable]
[NetSerializable]
public sealed class CameraKickEvent : EntityEventArgs
{
    public readonly NetEntity NetEntity;
    public readonly Vector2 Recoil;

    public CameraKickEvent(NetEntity netEntity, Vector2 recoil)
    {
        Recoil = recoil;
        NetEntity = netEntity;
    }
}
