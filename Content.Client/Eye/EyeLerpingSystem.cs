using System.Numerics;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Physics;
using Robust.Client.Player;
using Robust.Shared.Timing;

namespace Content.Client.Eye;

public sealed class EyeLerpingSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    // Convenience variable for for VV.
    [ViewVariables, UsedImplicitly]
    private IEnumerable<LerpingEyeComponent> ActiveEyes => EntityQuery<LerpingEyeComponent>();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EyeComponent, ComponentStartup>(OnEyeStartup);
        SubscribeLocalEvent<EyeComponent, ComponentShutdown>(OnEyeShutdown);
        SubscribeLocalEvent<EyeAttachedEvent>(OnAttached);

        SubscribeLocalEvent<LerpingEyeComponent, EntParentChangedMessage>(HandleMapChange);
        SubscribeLocalEvent<LerpingEyeComponent, LocalPlayerDetachedEvent>(OnDetached);

        UpdatesAfter.Add(typeof(TransformSystem));
        UpdatesAfter.Add(typeof(PhysicsSystem));
        UpdatesBefore.Add(typeof(SharedEyeSystem));
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

        lerpInfo.TargetZoom = component.Zoom;
        lerpInfo.LastZoom = lerpInfo.TargetZoom;

        if (component.Eye != null)
        {
            _eye.SetRotation(uid, lerpInfo.TargetRotation, component);
            _eye.SetZoom(uid, lerpInfo.TargetZoom, component);
        }
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

    private void OnAttached(ref EyeAttachedEvent ev)
    {
        AddEye(ev.Entity, ev.Component, true);
    }

    private void OnDetached(EntityUid uid, LerpingEyeComponent component, LocalPlayerDetachedEvent args)
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
        var query = AllEntityQuery<LerpingEyeComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var lerpInfo, out var xform))
        {
            lerpInfo.LastRotation = lerpInfo.TargetRotation;
            lerpInfo.TargetRotation = GetRotation(uid, xform);

            lerpInfo.LastZoom = lerpInfo.TargetZoom;
            lerpInfo.TargetZoom = UpdateZoom(uid, frameTime);
        }
    }

    private Vector2 UpdateZoom(EntityUid uid, float frameTime, EyeComponent? eye = null, ContentEyeComponent? content = null)
    {
        if (!Resolve(uid, ref content, ref eye, false))
            return Vector2.One;

        var diff = content.TargetZoom - eye.Zoom;

        if (diff.LengthSquared() < 0.00001f)
        {
            return content.TargetZoom;
        }

        var change = diff * Math.Min(8f * frameTime, 1);

        return eye.Zoom + change;
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
            return -_transform.GetWorldRotation(relative.Value);

        return Angle.Zero;
    }

    public override void FrameUpdate(float frameTime)
    {
        var tickFraction = (float) _gameTiming.TickFraction / ushort.MaxValue;
        const double lerpMinimum = 0.00001;
        var query = AllEntityQuery<LerpingEyeComponent, EyeComponent, TransformComponent>();

        while (query.MoveNext(out var entity, out var lerpInfo, out var eye, out var xform))
        {
            // Handle zoom
            var zoomDiff = Vector2.Lerp(lerpInfo.LastZoom, lerpInfo.TargetZoom, tickFraction);

            if ((zoomDiff - lerpInfo.TargetZoom).Length() < lerpMinimum)
            {
                _eye.SetZoom(entity, lerpInfo.TargetZoom, eye);
            }
            else
            {
                _eye.SetZoom(entity, zoomDiff, eye);
            }

            // Handle Rotation
            TryComp<InputMoverComponent>(entity, out var mover);

            // This needs to be recomputed every frame, as if this is simply the grid rotation, then we need to account for grid angle lerping.
            lerpInfo.TargetRotation = GetRotation(entity, xform, mover);

            if (!NeedsLerp(mover))
            {
                _eye.SetRotation(entity, lerpInfo.TargetRotation, eye);
                continue;
            }

            var shortest = Angle.ShortestDistance(lerpInfo.LastRotation, lerpInfo.TargetRotation);

            if (Math.Abs(shortest.Theta) < lerpMinimum)
            {
                _eye.SetRotation(entity, lerpInfo.TargetRotation, eye);
                continue;
            }

            _eye.SetRotation(entity, shortest * tickFraction + lerpInfo.LastRotation, eye);
        }
    }
}
