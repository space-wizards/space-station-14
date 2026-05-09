using Content.Server.Radiation.Components;
using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Events;
using Content.Shared.Radiation.Systems;
using Content.Shared.Stacks;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Radiation.Systems;

public sealed partial class RadiationSystem : SharedRadiationSystem
{
    [Dependency] private IMapManager _mapManager = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedStackSystem _stack = default!;
    [Dependency] private SharedMapSystem _maps = default!;

    [Dependency] private EntityQuery<RadiationBlockingContainerComponent> _blockerQuery = default!;
    [Dependency] private EntityQuery<RadiationGridResistanceComponent> _resistanceQuery = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;

    private float _accumulator;
    private List<SourceData> _sources = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeCvars();
        InitRadBlocking();
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

        entity.Comp.Enabled = val;
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
