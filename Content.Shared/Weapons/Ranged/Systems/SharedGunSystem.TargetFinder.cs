using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Weapons.Hitscan.Events;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Shared.Weapons.Ranged.Systems;

public partial class SharedGunSystem
{
    [Dependency] private SharedDeviceLinkSystem _deviceLink = default!;

    [SubscribeLocalEvent]
    public void OnHitscanHit(Entity<TargetFinderHitscanComponent> entity, ref HitscanRaycastFiredEvent args)
    {
        if (!HasComp<TargetFinderComponent>(args.Data.Gun))
            return;

        UpdateTarget(args.Data.Gun, args.Data.HitEntity);
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<TargetFinderComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<DeviceLinkSourceComponent>(ent, out var source))
            return;

        var linkedEntities = _deviceLink.GetLinkedSinks((ent.Owner, source), ent.Comp.LinkingPort);

        foreach (var sink in linkedEntities)
        {
            if (!TryComp<TargetAssignComponent>(sink, out var targetAssigner))
                continue;

            ent.Comp.TargetAssigner = sink;
            break;
        }
    }

    [SubscribeLocalEvent]
    private void OnNewLink(Entity<TargetFinderComponent> ent, ref NewLinkEvent args)
    {
        if (args.SourcePort != ent.Comp.LinkingPort || !HasComp<TargetAssignComponent>(args.Sink))
            return;

        ent.Comp.TargetAssigner = args.Sink;
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnPortDisconnected(Entity<TargetFinderComponent> ent, ref PortDisconnectedEvent args)
    {
        if (args.Port != ent.Comp.LinkingPort || ent.Comp.TargetAssigner == null)
            return;

        ent.Comp.TargetAssigner = null;
        Dirty(ent);
    }

    private void UpdateTarget(Entity<TargetFinderComponent?> ent, EntityUid? target)
    {
        if (!Resolve(ent, ref ent.Comp, false) || target is null)
            return;

        ent.Comp.Target = target;

        if (ent.Comp.TargetAssigner is null ||
            !TryComp<TargetAssignComponent>(ent.Comp.TargetAssigner, out var targetAssignComp))
            return;

        targetAssignComp.Target = target;
    }
}
