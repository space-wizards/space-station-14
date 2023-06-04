using System.Runtime.InteropServices;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Follower.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
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

        // UpdatesOutsidePrediction = true;

        SubscribeLocalEvent<EyeProtectionComponent, GotEquippedEvent>(OnGlassesEquipped);
        SubscribeLocalEvent<EyeProtectionComponent, GotUnequippedEvent>(OnGlassesUnequipped);
    }

    private void OnGlassesEquipped(EntityUid uid, EyeProtectionComponent component, GotEquippedEvent args)
    {
        if ((args.SlotFlags & (SlotFlags.EYES | SlotFlags.MASK | SlotFlags.HEAD)) == 0)
            return;

        if (TryComp<EyeComponent>(args.Equipee, out var eyes) && eyes.AutoExpose != null && eyes.Eye != null)
        {
            // Putting on glasses makes everything darker, your nightvision will adjust but also suffer.
            // eyes.AutoExpose.Reduction += component.VisionDarken;
            // eyes.Eye.Exposure /= component.VisionDarken;
        }
    }

    private void OnGlassesUnequipped(EntityUid uid, EyeProtectionComponent component, GotUnequippedEvent args)
    {
        if ((args.SlotFlags & (SlotFlags.EYES | SlotFlags.MASK | SlotFlags.HEAD)) == 0)
            return;

        if (TryComp<EyeComponent>(uid, out var eyes) && eyes.AutoExpose != null && eyes.Eye != null)
        {
            // Removing on glasses makes everything lighter again.
            // eyes.AutoExpose.Reduction -= component.VisionDarken;
            // eyes.Eye.Exposure *= component.VisionDarken;
        }
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

        // Simulate an eye behind something that is reducing the light, for instance sunglasses.
        float rawExpose = eye.Exposure + auto.Reduction;

        // How close we are to full night vision as a ratio.
        float nightPortion = Math.Clamp(rawExpose / auto.Max, 0.0f, 1.0f);

        // Calculate how bright we want the centre of the screen to be. 1.0 is how bright the artist drew the sprites.
        //   We let the goal get slightly darker as the user enters their night vision.
        var goalBright = MathHelper.Lerp(auto.GoalBrightness, auto.GoalBrightnessNight, nightPortion);

        // How much should we increase or decrease brightness as a ratio to land at 80% lighting?
        // By limiting the ranges goalChange can take on and avoiding div/0 here it is much easier to tune this.
        var goalChange = Math.Clamp(goalBright / Math.Max(0.0001f, auto.LastBrightness), 0.1f, 5.0f);
        var goalExposure = rawExpose * goalChange;

        if (goalChange < 1.0f)
        {
            // Reduce exposure
            var speed = MathHelper.Lerp(auto.RampDown, auto.RampDownNight, nightPortion);
            rawExpose = MathHelper.Lerp(rawExpose, goalExposure, frameTime * auto.RampDown * 0.2f);
        }
        else
        {
            // Increase exposure
            var speed = MathHelper.Lerp(auto.RampUp, auto.RampUpNight, nightPortion);
            rawExpose = MathHelper.Lerp(rawExpose, goalExposure, frameTime * speed * 0.2f);
        }

        // Reset
        if (float.IsNaN(rawExpose))
        {
            rawExpose = 1.0f;
        }
        // Clamp to a range
        rawExpose = Math.Clamp(rawExpose, auto.Min, auto.Max);
        eye.Exposure = rawExpose - auto.Reduction;
    }

}
