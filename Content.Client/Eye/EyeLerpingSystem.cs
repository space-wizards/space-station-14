using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Physics;
using Robust.Client.Player;
using Robust.Shared.Collections;
using Robust.Shared.Timing;

namespace Content.Client.Eye;

public sealed class EyeLerpingSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;

    // Convenience variable for for VV.
    [ViewVariables]
    private IEnumerable<LerpingEyeComponent> ActiveEyes => EntityQuery<LerpingEyeComponent>();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EyeComponent, ComponentStartup>(OnEyeStartup);
        SubscribeLocalEvent<EyeComponent, ComponentShutdown>(OnEyeShutdown);
        SubscribeLocalEvent<LerpingEyeComponent, EntParentChangedMessage>(HandleMapChange);
        SubscribeLocalEvent<EyeComponent, PlayerAttachedEvent>(OnAttached);
        SubscribeLocalEvent<LerpingEyeComponent, PlayerDetachedEvent>(OnDetached);

        UpdatesAfter.Add(typeof(TransformSystem));
        UpdatesAfter.Add(typeof(PhysicsSystem));
        UpdatesBefore.Add(typeof(EyeUpdateSystem));
        UpdatesOutsidePrediction = true;
    }

    private void OnEyeStartup(EntityUid uid, EyeComponent component, ComponentStartup args)
    {
        if (_playerManager.LocalPlayer?.ControlledEntity == uid)
            AddEye(uid, component, true);
    }

    private void OnEyeShutdown(EntityUid uid, EyeComponent component, ComponentShutdown args)
    {
        RemCompDeferred<LerpingEyeComponent>(uid);
    }

    // TODO replace this with some way of automatically getting and including any eyes that are associated with a viewport / render able thingy.
    public void AddEye(EntityUid uid, EyeComponent? component = null, bool automatic = false)
    {
        if (!Resolve(uid, ref component))
            return;

        var lerpInfo = EnsureComp<LerpingEyeComponent>(uid);
        lerpInfo.TargetRotation = GetRotation(uid);
        lerpInfo.LastRotation = lerpInfo.TargetRotation;
        lerpInfo.ManuallyAdded |= !automatic;

        if (component.Eye != null)
            component.Eye.Rotation = lerpInfo.TargetRotation;
    }

    public void RemoveEye(EntityUid uid)
    {
        if (!TryComp(uid, out LerpingEyeComponent? lerp))
            return;

        // If this is the currently controlled entity, we keep the component.
        if (_playerManager.LocalPlayer?.ControlledEntity == uid)
            lerp.ManuallyAdded = false;
        else
            RemComp(uid, lerp);
    }

    private void HandleMapChange(EntityUid uid, LerpingEyeComponent component, ref EntParentChangedMessage args)
    {
        // Is this actually a map change? If yes, stop any lerps
        if (args.OldMapId != args.Transform.MapID)
            component.LastRotation = GetRotation(uid, args.Transform);
    }

    private void OnAttached(EntityUid uid, EyeComponent component, PlayerAttachedEvent args)
    {
        AddEye(uid, component, true);
    }

    private void OnDetached(EntityUid uid, LerpingEyeComponent component, PlayerDetachedEvent args)
    {
        if (!component.ManuallyAdded)
            RemCompDeferred(uid, component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_gameTiming.IsFirstTimePredicted)
            return;

        // Set all of our eye rotations to the relevant values.
        foreach (var (lerpInfo, xform) in EntityQuery<LerpingEyeComponent, TransformComponent>())
        {
            lerpInfo.LastRotation = lerpInfo.TargetRotation;
            lerpInfo.TargetRotation = GetRotation(lerpInfo.Owner, xform);
        }
    }

    /// <summary>
    /// Does the eye need to lerp or is its rotation matched.
    /// </summary>
    private bool NeedsLerp(InputMoverComponent? mover)
    {
        if (mover == null)
            return false;

        if (mover.RelativeRotation.Equals(mover.TargetRelativeRotation))
            return false;

        return true;
    }

    private Angle GetRotation(EntityUid uid, TransformComponent? xform = null, InputMoverComponent? mover = null)
    {
        if (!Resolve(uid, ref xform))
            return Angle.Zero;

        // If we can move then tie our eye to our inputs (these also get lerped so it should be fine).
        if (Resolve(uid, ref mover, false))
        {
            return -_mover.GetParentGridAngle(mover);
        }

        // if not tied to a mover then lock it to map / grid
        var relative = xform.GridUid ?? xform.MapUid;
        if (relative != null)
            return -Transform(relative.Value).WorldRotation;

        return Angle.Zero;
    }

    public override void FrameUpdate(float frameTime)
    {
        var tickFraction = (float) _gameTiming.TickFraction / ushort.MaxValue;
        const double lerpMinimum = 0.00001;

        foreach (var (lerpInfo, eye, xform) in EntityQuery<LerpingEyeComponent, EyeComponent, TransformComponent>())
        {
            var entity = eye.Owner;

            TryComp<InputMoverComponent>(entity, out var mover);

            // This needs to be recomputed every frame, as if this is simply the grid rotation, then we need to account for grid angle lerping.
            lerpInfo.TargetRotation = GetRotation(entity, xform, mover);

            if (!NeedsLerp(mover))
            {
                eye.Rotation = lerpInfo.TargetRotation;
                continue;
            }

            var shortest = Angle.ShortestDistance(lerpInfo.LastRotation, lerpInfo.TargetRotation);

            if (Math.Abs(shortest.Theta) < lerpMinimum)
            {
                eye.Rotation = lerpInfo.TargetRotation;
                continue;
            }

            eye.Rotation = shortest * tickFraction + lerpInfo.LastRotation;
        }
    }
}
