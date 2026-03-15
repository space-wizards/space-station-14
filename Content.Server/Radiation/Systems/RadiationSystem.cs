using Content.Server.Radiation.Components;
using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Events;
using Content.Shared.Stacks;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Threading;
using System.Numerics;
using System.Threading;

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

    private readonly B2DynamicTree<EntityUid> _sourceTree = new();
    private readonly Dictionary<EntityUid, SourceData> _sourceDataMap = new();
    private readonly List<EntityUid> _activeReceivers = new();

    private static readonly ThreadLocal<List<Entity<MapGridComponent>>> _gridListCache
        = new(() => new List<Entity<MapGridComponent>>(8));
    private static readonly ThreadLocal<List<EntityUid>> _nearbySourcesCache
        = new(() => new List<EntityUid>(64));

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
        if (component.Proxy != DynamicTree.Proxy.Free)
        {
            _sourceTree.DestroyProxy(component.Proxy);
            component.Proxy = DynamicTree.Proxy.Free;
        }
        _sourceDataMap.Remove(uid);
    }

    private void OnSourceMove(Entity<RadiationSourceComponent> entity, ref MoveEvent args)
    {
        if (args.NewPosition.EntityId == args.OldPosition.EntityId &&
            args.NewPosition.Position.EqualsApprox(args.OldPosition.Position))
            return;

        UpdateSource(entity);
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

    private void UpdateSource(Entity<RadiationSourceComponent> entity)
    {
        var (uid, component) = entity;
        var xform = Transform(uid);

        if (!component.Enabled || Terminating(uid))
        {
            if (component.Proxy != DynamicTree.Proxy.Free)
            {
                _sourceTree.DestroyProxy(component.Proxy);
                component.Proxy = DynamicTree.Proxy.Free;
            }
            _sourceDataMap.Remove(uid);
            return;
        }

        var worldPos = _transform.GetWorldPosition(xform);
        var intensity = component.Intensity * _stack.GetCount(uid);
        intensity = GetAdjustedRadiationIntensity(uid, intensity);

        if (intensity <= 0)
        {
            if (component.Proxy != DynamicTree.Proxy.Free)
            {
                _sourceTree.DestroyProxy(component.Proxy);
                component.Proxy = DynamicTree.Proxy.Free;
            }
            _sourceDataMap.Remove(uid);
            return;
        }

        // Avoid division by 0
        var maxRange = component.Slope >= float.Epsilon ? intensity / component.Slope : GridcastMaxDistance;
        maxRange = Math.Min(maxRange, GridcastMaxDistance);

        _sourceDataMap[uid] = new SourceData(intensity, component.Slope, maxRange, (uid, component, xform), worldPos);
        var aabb = Box2.CenteredAround(worldPos, new Vector2(maxRange * 2, maxRange * 2));

        if (component.Proxy != DynamicTree.Proxy.Free)
        {
            _sourceTree.MoveProxy(component.Proxy, in aabb);
        }
        else
        {
            component.Proxy = _sourceTree.CreateProxy(in aabb, uint.MaxValue, uid);
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

    public void IrradiateEntity(EntityUid uid, float radsPerSecond, float time, EntityUid? origin = null)
    {
        var msg = new OnIrradiatedEvent(time, radsPerSecond, origin);
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
