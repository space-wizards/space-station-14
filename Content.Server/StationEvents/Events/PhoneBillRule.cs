using Content.Server.PhoneBill;
using Content.Server.Stack;
using Content.Server.StationEvents.Components;
using Content.Shared.Cargo.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Stacks;
using Robust.Server.Containers;
using Robust.Shared.Containers;

namespace Content.Server.StationEvents.Events;

public sealed class PhoneBillRule : StationEventSystem<PhoneBillRuleComponent>
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly ContainerSystem _containerSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly StackSystem _stackSystem = default!;

    protected override void Added(EntityUid uid, PhoneBillRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);
        ChatSystem.DispatchGlobalAnnouncement( Loc.GetString("station-event-phone-bill-announcement", ("delay", (int)component.Delay.TotalSeconds), ("price", component.Price)), announcementSound: component.InitialSound, colorOverride: Color.Red);

        List<EntityUid> PhoneBillable = new();

        var billableQuery = _entMan.AllEntityQueryEnumerator<PhoneBillableComponent>();
        while (billableQuery.MoveNext(out var target, out var billable))
        {
            if (billable.RequireLiving &&
                TryComp(uid, out MobStateComponent? mobState) &&
                mobState.CurrentState != MobState.Alive)
                continue;
            PhoneBillable.Add(target);
        }

        var billTargetQuery = _entMan.AllEntityQueryEnumerator<PhoneBillTargetComponent>();
        while (billTargetQuery.MoveNext(out var target, out var _))
        {
            var lastTarget = target;
            while (_containerSystem.TryGetContainingContainer((lastTarget, null, null), out var container))
            {
                if (PhoneBillable.Contains(container.Owner))
                {
                    if (!component.YoullPayForThis.TryGetValue(container.Owner, out var list))
                    {
                        list = new();
                        component.YoullPayForThis[container.Owner] = list;
                    }
                    list.Add(target);
                    break;
                }

                lastTarget = container.Owner;
            }
        }
    }

    protected override void Started(EntityUid uid, PhoneBillRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        if (component.YoullPayForThis.Count == 0)
            return; // Huh.

        int unpaid = 0;
        foreach (var (forsaken, items) in component.YoullPayForThis)
        {
            var tendered = false;
            var required = component.Price * items.Count;
            foreach (var ransom in _inventorySystem.GetHandOrInventoryEntities(forsaken))
            {
                if (HasComp(ransom, typeof(CashComponent))
                    && TryComp(ransom, out StackComponent? stack)
                    && stack.Count >= required)
                {
                    _stackSystem.SetCount(ransom, stack.Count - required);
                    tendered = true;
                    break;
                }
            }
            if (tendered)
                continue;

            // THEN PAY WITH YOUR BLOOD!

            unpaid++;

            foreach (var item in items)
            {
                if (HasComp<ContainerManagerComponent>(item))
                    foreach (var container in _containerSystem.GetAllContainers(item))
                    {
                        _containerSystem.EmptyContainer(container);
                    }
                _entMan.QueueDeleteEntity(item);
            }
        }

        if (unpaid == 0)
        {
            ChatSystem.DispatchGlobalAnnouncement(Loc.GetString("station-event-phone-bill-allpaid-announcement"), announcementSound: component.SuccessSound);
        }
        else
        {
            var unpaidPercent = (int)(100f * unpaid / component.YoullPayForThis.Count);
            ChatSystem.DispatchGlobalAnnouncement(Loc.GetString("station-event-phone-bill-unpaid-announcement", ("percent", unpaidPercent)), announcementSound: component.FailureSound, colorOverride: Color.Yellow);
        }
    }
}
