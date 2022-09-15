using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Physics;
using Robust.Client.Player;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Eye;

public sealed class EyeLerpingSystem : EntitySystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;

    // Eyes other than the primary eye that are currently active.
    private readonly Dictionary<EntityUid, EyeLerpInformation> _activeEyes = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EyeComponent, ComponentStartup>(OnEyeStartup);
        SubscribeLocalEvent<EyeComponent, ComponentShutdown>(OnEyeShutdown);

        UpdatesAfter.Add(typeof(TransformSystem));
        UpdatesAfter.Add(typeof(PhysicsSystem));
        UpdatesBefore.Add(typeof(EyeUpdateSystem));
    }

    private void OnEyeStartup(EntityUid uid, EyeComponent component, ComponentStartup args)
    {
        if (component.Eye == null)
            return;

        // If the eye starts up then don't lerp at all.
        var xformQuery = GetEntityQuery<TransformComponent>();
        TryComp<InputMoverComponent>(uid, out var mover);
        xformQuery.TryGetComponent(uid, out var xform);
        var lerpInfo = _activeEyes.GetOrNew(uid);
        lerpInfo.TargetRotation = GetRotation(xformQuery, mover, xform);
        lerpInfo.LastRotation = lerpInfo.TargetRotation;

        if (xform != null)
        {
            lerpInfo.MapId = xform.MapID;
        }

        component.Eye.Rotation = lerpInfo.TargetRotation;
    }

    private void OnEyeShutdown(EntityUid uid, EyeComponent component, ComponentShutdown args)
    {
        RemoveEye(uid);
    }

    public void AddEye(EntityUid uid)
    {
        _activeEyes.TryAdd(uid, new EyeLerpInformation());
    }

    public void RemoveEye(EntityUid uid)
    {
        _activeEyes.Remove(uid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_gameTiming.IsFirstTimePredicted)
            return;

        var moverQuery = GetEntityQuery<InputMoverComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();
        var foundEyes = new ValueList<EntityUid>(1);

        // Set all of our eye rotations to the relevant values.
        foreach (var (eye, entity) in GetEyes())
        {
            var lerpInfo = _activeEyes.GetOrNew(entity);
            foundEyes.Add(entity);
            moverQuery.TryGetComponent(entity, out var mover);
            xformQuery.TryGetComponent(entity, out var xform);
            lerpInfo.LastRotation = eye.Rotation;
            lerpInfo.TargetRotation = GetRotation(xformQuery, mover, xform);

            if (xform != null)
            {
                // If we traverse maps then don't lerp.
                if (xform.MapID != lerpInfo.MapId)
                {
                    lerpInfo.LastRotation = lerpInfo.TargetRotation;
                }
            }
        }

        foreach (var eye in foundEyes)
        {
            if (!_activeEyes.ContainsKey(eye))
            {
                _activeEyes.Remove(eye);
            }
        }
    }

    private Angle GetRotation(EntityQuery<TransformComponent> xformQuery, InputMoverComponent? mover = null, TransformComponent? xform = null)
    {
        // If we can move then tie our eye to our inputs (these also get lerped so it should be fine).
        if (mover != null)
        {
            return -_mover.GetParentGridAngle(mover);
        }

        // if not tied to a mover then lock it to map / grid
        if (xform != null)
        {
            var relative = xform.GridUid;
            relative ??= xform.MapUid;

            if (xformQuery.TryGetComponent(relative, out var relativeXform))
            {
                return -relativeXform.WorldRotation;
            }
        }

        return Angle.Zero;
    }

    private IEnumerable<(IEye Eye, EntityUid Entity)> GetEyes()
    {
        if (_playerManager.LocalPlayer?.ControlledEntity is { } player && !Deleted(player))
        {
            yield return (_eyeManager.CurrentEye, player);
        }

        if (_activeEyes.Count == 0)
            yield break;

        var eyeQuery = GetEntityQuery<EyeComponent>();

        foreach (var (ent, info) in _activeEyes)
        {
            if (!eyeQuery.TryGetComponent(ent, out var eyeComp) ||
                eyeComp.Eye == null)
            {
                continue;
            }

            yield return (eyeComp.Eye, ent);
        }
    }

    public override void FrameUpdate(float frameTime)
    {
        var tickFraction = (float) _gameTiming.TickFraction / ushort.MaxValue;
        const double lerpMinimum = 0.00001;

        foreach (var (eye, entity) in GetEyes())
        {
            if (!_activeEyes.TryGetValue(entity, out var lerpInfo))
                continue;

            var shortest = Angle.ShortestDistance(lerpInfo.LastRotation, lerpInfo.TargetRotation);

            if (Math.Abs(shortest.Theta) < lerpMinimum)
            {
                eye.Rotation = lerpInfo.TargetRotation;
                continue;
            }

            eye.Rotation = shortest * tickFraction + lerpInfo.LastRotation;
        }
    }

    private sealed class EyeLerpInformation
    {
        public Angle LastRotation;
        public Angle TargetRotation;

        /// <summary>
        /// If we go to a new map then don't lerp and snap instantly.
        /// </summary>
        public MapId MapId;
    }
}
