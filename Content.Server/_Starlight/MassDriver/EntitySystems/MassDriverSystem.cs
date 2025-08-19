using Content.Shared._Starlight.MassDriver.Components;
using Content.Shared._Starlight.MassDriver;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;
using Robust.Shared.GameObjects;
using System.Linq;
using Content.Shared.Power;
using Content.Server.Power.EntitySystems;

namespace Content.Server._Starlight.MassDriver.EntitySystems;

public sealed class MassDriverSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming Timing = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly PowerReceiverSystem _powerReceiver = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MassDriverComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnPowerChanged(EntityUid uid, MassDriverComponent component, ref PowerChangedEvent args)
    {
        if (component.Mode != MassDriverMode.Auto)
            return;

        var hasComp = HasComp<ActiveMassDriverComponent>(uid);
        if (hasComp && !args.Powered)
            RemComp<ActiveMassDriverComponent>(uid);
        else if (!hasComp && args.Powered)
            EnsureComp<ActiveMassDriverComponent>(uid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveMassDriverComponent, MassDriverComponent>();
        while (query.MoveNext(out var uid, out var activeMassDriver, out var massDriver))
        {
            if (Timing.CurTime < activeMassDriver.NextUpdateTime)
                continue;

            if (activeMassDriver.NextUpdateTime == TimeSpan.Zero)
            {
                activeMassDriver.NextUpdateTime = Timing.CurTime + activeMassDriver.UpdateDelay;
                continue;
            }
            activeMassDriver.NextUpdateTime = Timing.CurTime + activeMassDriver.UpdateDelay;

            if (!_powerReceiver.IsPowered(uid))
                continue;

            var entities = new HashSet<EntityUid>();
            _lookup.GetEntitiesIntersecting(uid, entities);
            var entitiesCount = entities.Count(a => !Transform(a).Anchored);

            if (entitiesCount == 0)
            {
                if (activeMassDriver.NextThrowTime != TimeSpan.Zero)
                {
                    activeMassDriver.NextThrowTime = TimeSpan.Zero;
                    _appearance.SetData(uid, MassDriverVisuals.Launching, false);
                }
                if (massDriver.Mode == MassDriverMode.Manual)
                    RemComp<ActiveMassDriverComponent>(uid);
                continue;
            }

            if (activeMassDriver.NextThrowTime == TimeSpan.Zero)
            {
                activeMassDriver.NextThrowTime = Timing.CurTime + massDriver.ThrowDelay;
                continue;
            }
            else if (Timing.CurTime < activeMassDriver.NextThrowTime)
                continue;

            _appearance.SetData(uid, MassDriverVisuals.Launching, true);

            var xform = Transform(uid);
            var throwing = xform.LocalRotation.ToWorldVec() * (massDriver.CurrentThrowDistance - (massDriver.ThrowCountDelta * (entities.Count - 1)));
            var direction = xform.Coordinates.Offset(throwing);
            var speed = massDriver.CurrentThrowSpeed - (massDriver.ThrowCountDelta * (entitiesCount - 1));

            foreach (var entity in entities)
                _throwing.TryThrow(entity, direction, speed);
        }
    }
}