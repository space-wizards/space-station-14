// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Drones.Components;
using Content.Shared.Actions;
using Content.Shared.Mobs;
using Content.Shared.Sticky;
using Content.Shared.Sticky.Components;
using Content.Shared.Sticky.Systems;
using Content.Shared.Trigger.Systems;
using Content.Shared.Whitelist;

namespace Content.Server.DeadSpace.Drones.Systems;

public sealed class DetonateAttachedExplosivesSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;
    [Dependency] private readonly StickySystem _sticky = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DetonateAttachedExplosivesComponent, DetonateAttachedExplosivesActionEvent>(OnDetonate);
        SubscribeLocalEvent<DetonateAttachedExplosivesComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<DetonateAttachedExplosivesComponent, EntityTerminatingEvent>(OnTerminating);
        SubscribeLocalEvent<StickyComponent, EntityStuckEvent>(OnExplosiveStuck);
        SubscribeLocalEvent<StickyComponent, EntityUnstuckEvent>(OnExplosiveUnstuck);
    }

    private void OnExplosiveStuck(Entity<StickyComponent> ent, ref EntityStuckEvent args)
    {
        if (!TryComp<DetonateAttachedExplosivesComponent>(args.Target, out var detonate) ||
            !_whitelist.IsWhitelistPassOrNull(detonate.ExplosiveWhitelist, ent.Owner) ||
            detonate.ActionEntity != null)
            return;

        _actions.AddAction(args.Target, ref detonate.ActionEntity, detonate.Action);
        if (detonate.ActionEntity is not { } action ||
            !TryComp<DroneComponent>(args.Target, out var drone))
            return;

        drone.ActionEntities.Add(action);
        if (drone.DroneHost is { } host)
            _actions.AddAction(host, action, args.Target);
    }

    private void OnExplosiveUnstuck(Entity<StickyComponent> ent, ref EntityUnstuckEvent args)
    {
        if (!TryComp<DetonateAttachedExplosivesComponent>(args.Target, out var detonate) ||
            HasAttachedExplosive(args.Target, detonate, ent.Owner))
            return;

        RemoveDetonateAction(args.Target, detonate);
    }

    private void OnDetonate(Entity<DetonateAttachedExplosivesComponent> ent,
        ref DetonateAttachedExplosivesActionEvent args)
    {
        if (args.Handled)
            return;

        var detonated = false;
        var query = EntityQueryEnumerator<StickyComponent>();
        while (query.MoveNext(out var explosive, out var sticky))
        {
            if (sticky.StuckTo != ent.Owner ||
                !_whitelist.IsWhitelistPassOrNull(ent.Comp.ExplosiveWhitelist, explosive))
                continue;

            detonated |= _trigger.Trigger(explosive, args.Performer, "timer");
        }

        args.Handled = detonated;
    }

    private void OnTerminating(Entity<DetonateAttachedExplosivesComponent> ent, ref EntityTerminatingEvent args)
    {
        RemoveDetonateAction(ent.Owner, ent.Comp);
        DetachExplosives(ent);
    }

    private void OnMobStateChanged(Entity<DetonateAttachedExplosivesComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            DetachExplosives(ent);
    }

    private void DetachExplosives(Entity<DetonateAttachedExplosivesComponent> ent)
    {
        var query = EntityQueryEnumerator<StickyComponent>();
        while (query.MoveNext(out var explosive, out var sticky))
        {
            if (sticky.StuckTo != ent.Owner ||
                !_whitelist.IsWhitelistPassOrNull(ent.Comp.ExplosiveWhitelist, explosive))
                continue;

            _sticky.UnstickFromEntity((explosive, sticky), ent.Owner);
        }
    }

    private bool HasAttachedExplosive(EntityUid target,
        DetonateAttachedExplosivesComponent component,
        EntityUid ignored)
    {
        var query = EntityQueryEnumerator<StickyComponent>();
        while (query.MoveNext(out var explosive, out var sticky))
        {
            if (explosive != ignored &&
                sticky.StuckTo == target &&
                _whitelist.IsWhitelistPassOrNull(component.ExplosiveWhitelist, explosive))
                return true;
        }

        return false;
    }

    private void RemoveDetonateAction(EntityUid owner, DetonateAttachedExplosivesComponent component)
    {
        if (component.ActionEntity is not { } action)
            return;

        if (TryComp<DroneComponent>(owner, out var drone))
            drone.ActionEntities.Remove(action);

        _actions.RemoveAction(action);
        component.ActionEntity = null;
    }
}
