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

    private void OnSourceInit(EntityUid uid, RadiationSourceComponent component, ComponentInit args)
    {
        UpdateSource(uid, component);
    }

    private void OnSourceShutdown(EntityUid uid, RadiationSourceComponent component, ComponentShutdown args)
    {
        if (_sourceDataMap.Remove(uid))
        {
            _sourceTree.Remove(uid);
        }
    }

    private void OnSourceMove(EntityUid uid, RadiationSourceComponent component, ref MoveEvent args)
    {
        if (args.NewPosition.EntityId == args.OldPosition.EntityId &&
            args.NewPosition.Position.EqualsApprox(args.OldPosition.Position))
            return;

        UpdateSource(uid, component, args.Component);
    }

    private void OnSourceStackChanged(EntityUid uid, RadiationSourceComponent component, StackCountChangedEvent args)
    {
        UpdateSource(uid, component);
    }

    private void OnReceiverInit(EntityUid uid, RadiationReceiverComponent component, ComponentInit args)
    {
        _activeReceivers.Add(uid);
    }

    private void OnReceiverShutdown(EntityUid uid, RadiationReceiverComponent component, ComponentShutdown args)
    {
        _activeReceivers.Remove(uid);
    }

    private void UpdateSource(EntityUid uid, RadiationSourceComponent component, TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref xform))
            return;

        if (!component.Enabled || Terminating(uid))
        {
            if (_sourceDataMap.Remove(uid)) _sourceTree.Remove(uid);
            return;
        }

        var worldPos = _transform.GetWorldPosition(xform);
        var intensity = component.Intensity * _stack.GetCount(uid);
        intensity = GetAdjustedRadiationIntensity(uid, intensity);

        if (intensity <= 0)
        {
            if (_sourceDataMap.Remove(uid)) _sourceTree.Remove(uid);
            return;
        }

        var maxRange = component.Slope > 1e-6f ? intensity / component.Slope : GridcastMaxDistance;
        maxRange = Math.Min(maxRange, GridcastMaxDistance);

        var sourceData = new SourceData(intensity, component.Slope, maxRange, (uid, component, xform), worldPos);
        var aabb = Box2.CenteredAround(worldPos, new Vector2(maxRange * 2, maxRange * 2));

        if (_sourceDataMap.ContainsKey(uid))
        {
            _sourceDataMap[uid] = sourceData;
            _sourceTree.Update(uid, aabb);
        }
        else
        {
            _sourceDataMap.Add(uid, sourceData);
            _sourceTree.Add(uid, aabb);
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

        entity.Comp.Enabled = val;
        UpdateSource(entity, entity.Comp);
    }

    /// <summary>
    ///     Marks entity to receive/ignore radiation rays.
    /// </summary>
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
