using Content.Server.Radiation.Components;
using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Events;
using Content.Shared.Stacks;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Threading;
using System.Numerics;

namespace Content.Server.Radiation.Systems;

public sealed partial class RadiationSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly IParallelManager _parallel = default!;

    private EntityQuery<RadiationBlockingContainerComponent> _blockerQuery;
    private EntityQuery<RadiationGridResistanceComponent> _resistanceQuery;
    private EntityQuery<StackComponent> _stackQuery;

    private readonly DynamicTree<EntityUid> _sourceTree = new(static (in EntityUid _) => default, EqualityComparer<EntityUid>.Default);
    private readonly Dictionary<EntityUid, SourceData> _sourceDataMap = new();
    private readonly List<EntityUid> _activeReceivers = new();

    private float _accumulator;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeCvars();
        InitRadBlocking();

        _blockerQuery = GetEntityQuery<RadiationBlockingContainerComponent>();
        _resistanceQuery = GetEntityQuery<RadiationGridResistanceComponent>();
        _stackQuery = GetEntityQuery<StackComponent>();

        SubscribeLocalEvent<RadiationSourceComponent, ComponentInit>(OnSourceInit);
        SubscribeLocalEvent<RadiationSourceComponent, ComponentShutdown>(OnSourceShutdown);
        SubscribeLocalEvent<RadiationSourceComponent, MoveEvent>(OnSourceMove);
        SubscribeLocalEvent<RadiationSourceComponent, StackCountChangedEvent>(OnSourceStackChanged);

        SubscribeLocalEvent<RadiationReceiverComponent, ComponentInit>(OnReceiverInit);
        SubscribeLocalEvent<RadiationReceiverComponent, ComponentShutdown>(OnReceiverShutdown);
    }

    private void OnSourceInit(Entity<RadiationSourceComponent> entity, ref ComponentInit args)
    {
        UpdateSource(entity);
    }

    private void OnSourceShutdown(EntityUid uid, RadiationSourceComponent component, ComponentShutdown args)
    {
        if (_sourceDataMap.Remove(uid))
        {
            _sourceTree.Remove(uid);
        }
    }

    private void OnSourceMove(Entity<RadiationSourceComponent> entity, ref MoveEvent args)
    {
        if (args.NewPosition.EntityId == args.OldPosition.EntityId &&
            args.NewPosition.Position.EqualsApprox(args.OldPosition.Position))
            return;

        UpdateSource(entity, args.Component);
    }

    private void OnSourceStackChanged(Entity<RadiationSourceComponent> entity, ref StackCountChangedEvent args)
    {
        UpdateSource(entity);
    }

    private void OnReceiverInit(EntityUid uid, RadiationReceiverComponent component, ComponentInit args)
    {
        _activeReceivers.Add(uid);
    }

    private void OnReceiverShutdown(EntityUid uid, RadiationReceiverComponent component, ComponentShutdown args)
    {
        _activeReceivers.Remove(uid);
    }

    private void UpdateSource(Entity<RadiationSourceComponent> entity, TransformComponent? xform = null)
    {
        if (!Resolve(entity.Owner, ref xform))
            return;

        if (!entity.Comp.Enabled || Terminating(entity.Owner))
        {
            if (_sourceDataMap.Remove(entity.Owner))
                _sourceTree.Remove(entity.Owner);
            return;
        }

        var worldPos = _transform.GetWorldPosition(xform);
        var intensity = entity.Comp.Intensity * _stack.GetCount(entity.Owner);
        intensity = GetAdjustedRadiationIntensity(entity.Owner, intensity);

        if (intensity <= 0)
        {
            if (_sourceDataMap.Remove(entity.Owner))
                _sourceTree.Remove(entity.Owner);
            return;
        }

        var maxRange = entity.Comp.Slope > 1e-6f ? intensity / entity.Comp.Slope : GridcastMaxDistance;
        maxRange = Math.Min(maxRange, GridcastMaxDistance);

        var sourceData = new SourceData(intensity, entity.Comp.Slope, maxRange, (entity.Owner, entity.Comp, xform), worldPos);
        var aabb = Box2.CenteredAround(worldPos, new Vector2(maxRange * 2, maxRange * 2));

        if (_sourceDataMap.ContainsKey(entity.Owner))
        {
            _sourceDataMap[entity.Owner] = sourceData;
            _sourceTree.Update(entity.Owner, aabb);
        }
        else
        {
            _sourceDataMap.Add(entity.Owner, sourceData);
            _sourceTree.Add(entity.Owner, aabb);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _accumulator += frameTime;
        if (_accumulator < GridcastUpdateRate)
            return;

        UpdateGridcast();
        UpdateResistanceDebugOverlay();
        _accumulator = 0f;
    }

    public void IrradiateEntity(EntityUid uid, float radsPerSecond, float time)
    {
        var msg = new OnIrradiatedEvent(time, radsPerSecond, uid);
        RaiseLocalEvent(uid, msg);
    }

    public void SetSourceEnabled(Entity<RadiationSourceComponent?> entity, bool val)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return;
        if (entity.Comp.Enabled == val)
            return;

        entity.Comp.Enabled = val;
        UpdateSource((entity.Owner, entity.Comp));
    }

    public void SetCanReceive(EntityUid uid, bool canReceive)
    {
        if (canReceive)
        {
            EnsureComp<RadiationReceiverComponent>(uid);
        }
        else
        {
            RemComp<RadiationReceiverComponent>(uid);
        }
    }
}
