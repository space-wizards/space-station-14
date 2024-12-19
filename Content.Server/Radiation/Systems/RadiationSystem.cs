using Content.Server.Radiation.Components;
using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Events;
using Robust.Shared.Configuration;
using Robust.Shared.Map;

namespace Content.Server.Radiation.Systems;

public sealed partial class RadiationSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private float _accumulator;

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
