using System.Runtime.InteropServices;
using Content.Shared.Follower.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Physics;
using Robust.Client.Player;
using Robust.Shared.Collections;
using Robust.Shared.Timing;

namespace Content.Client.Eye;

public sealed class EyeExposureSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesOutsidePrediction = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateViewportExposure(frameTime);
    }

    private void UpdateViewportExposure(float frameTime)
    {
        if (!_entityManager.TryGetComponent(_playerManager.LocalPlayer?.ControlledEntity, out EyeComponent? eyeComp))
            return;

        if (eyeComp.Eye == null || eyeComp.Eye.AutoExpose == null)
            return;

        var eye = eyeComp.Eye;
        var auto = eye.AutoExpose;

        // How much should we increase or decrease brightness as a ratio to land at 80% lighting?
        // By limiting the ranges goalChange can take on and avoiding div/0 here it is much easier to tune this.
        var goalChange = Math.Clamp(auto.GoalBrightness / Math.Max(0.0001f, auto.LastBrightness), 0.2f, 5.0f);
        var goalExposure = eye.Exposure * goalChange;

        if (goalChange < 1.0f)
        {
            // Reduce exposure
            var speed = MathHelper.Lerp(auto.RampDown, auto.RampDownNight, Math.Clamp(eye.Exposure / auto.Max, 0.0f, 1.0f));
            eye.Exposure = MathHelper.Lerp(eye.Exposure, goalExposure, frameTime * auto.RampDown * 0.2f);
        }
        else
        {
            // Increase exposure
            var speed = MathHelper.Lerp(auto.RampUp, auto.RampUpNight, Math.Clamp(eye.Exposure / auto.Max, 0.0f, 1.0f));
            eye.Exposure = MathHelper.Lerp(eye.Exposure, goalExposure, frameTime * speed * 0.2f);
        }

        // Reset
        if (float.IsNaN(eye.Exposure))
        {
            eye.Exposure = 1.0f;
        }
        // Clamp to a range
        eye.Exposure = Math.Clamp(eye.Exposure, auto.Min, auto.Max);
    }

}
