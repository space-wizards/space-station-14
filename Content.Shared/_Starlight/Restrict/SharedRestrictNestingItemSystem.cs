using System.Linq;
using Content.Shared.ActionBlocker;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Movement.Components;
using Content.Shared.Popups;
using Content.Shared.Strip.Components;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Starlight.Restrict;
public abstract partial class SharedRestrictNestingItemSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    public override void Initialize()
    {
        //register a new verb for picking up the mob
        SubscribeLocalEvent<RestrictNestingItemComponent, GetVerbsEvent<InteractionVerb>>(AddPickupVerb);
        SubscribeLocalEvent<RestrictNestingItemComponent, RestrictNestingItemPickupDoAfterEvent>(FinishPickup);
        SubscribeLocalEvent<RestrictNestingItemComponent, DoAfterAttemptEvent<RestrictNestingItemPickupDoAfterEvent>>(DuringPickup);
        SubscribeLocalEvent<RestrictNestingItemComponent, StripAttemptEvent>(StripAttempt);
    }

    private void AddPickupVerb(Entity<RestrictNestingItemComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        //skip if its yourself
        if (args.Target == args.User)
            return;
        
        if (!InRange(args.User, args.Target))
            return;

        //make sure we arent in a container, if we are, skip showing the verb
        if (_containerSystem.TryGetContainingContainer((args.User, null, null), out var container))
            return;

        var user = args.User;
        var target = args.Target;
        var verb = new InteractionVerb()
        {
            Text = Loc.GetString("pick-up-verb-get-data-text"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/pickup.svg.192dpi.png")),
            Act = () => StartPickup(ent, user, target),
        };

        args.Verbs.Add(verb);
    }

    private bool InRange(EntityUid user, EntityUid target)
    {
        //check if nearby
        if (!_interactionSystem.InRangeAndAccessible(user, target) || !_actionBlockerSystem.CanInteract(user, target))
            return false;
        return true;
    }

    private void StripAttempt(Entity<RestrictNestingItemComponent> ent, ref StripAttemptEvent args)
    {
        //if we are already in a container
        if(_containerSystem.TryGetContainingContainer((args.Target, null, null), out var container) || 
           _containerSystem.TryGetContainingContainer((args.User, null, null), out var container2))
        {
            //check if the thing we are trying to insert is a nesting item
            if (RecursivelyCheckForNesting(args.Item, skipInitialItem: false))
            {
                //if it is, cancel the insert
                args.Cancel();
            }
        }
    }

    private void StartPickup(Entity<RestrictNestingItemComponent> ent, EntityUid user, EntityUid target)
    {
        if(_containerSystem.TryGetContainingContainer((user, null, null), out var container))
            return;
        
        //check range
        if (!InRange(user, target))
            return;

        //we need to recursively check inventory to see if the item being picked up has any other items that prevent nesting
        if (RecursivelyCheckForNesting(ent, skipInitialItem: true))
        {
            //if we find any, we need to cancel the pickup and show a popup message
            _popup.PopupClient(Loc.GetString("restrict-nesting-item-cant-pickup", ("user", ent)), user, user);
            return;
        }
        
        //start a doafter
        var doAfterEvent = new DoAfterArgs(EntityManager,
            user,
            ent.Comp.DoAfter,
            new RestrictNestingItemPickupDoAfterEvent(),
            ent)
            {
                AttemptFrequency = AttemptFrequency.EveryTick,
                BreakOnDamage = true,
                BreakOnMove = true,
                NeedHand = true,
                BreakOnHandChange = false
            };

        if(_doAfter.TryStartDoAfter(doAfterEvent))
        {
            //send a message to the player being picked up that someone is picking them up
            _popup.PopupEntity(Loc.GetString("restrict-nesting-item-pickup-start", ("user", user)), target, target, PopupType.Large);
        }
    }

    private void DuringPickup(Entity<RestrictNestingItemComponent> ent, ref DoAfterAttemptEvent<RestrictNestingItemPickupDoAfterEvent> ev)
    {
        var args = ev.Event.Args;

        //check if we are in a container, if we are, cancel the pickup
        if (_containerSystem.TryGetContainingContainer((args.User, null, null), out var container))
        {
            ev.Cancel();
            return;
        }

        //check range
        if (!InRange(args.User, ent))
        {
            ev.Cancel();
            return;
        }

        if (RecursivelyCheckForNesting(ent, skipInitialItem: true))
        {
            _popup.PopupEntity(Loc.GetString("restrict-nesting-item-cant-pickup", ("user", ent)), args.User, args.User);
            ev.Cancel();
            return;
        }
    }

    private void FinishPickup(Entity<RestrictNestingItemComponent> ent, ref RestrictNestingItemPickupDoAfterEvent args)
    {
        //check if cancelled
        if (args.Cancelled || args.Handled)
            return;

        //hacky solution for now, but if we are already in a container, then cancel
        if(_containerSystem.TryGetContainingContainer((args.User, null, null), out var container))
            return;
        
        //check range
        if (!InRange(args.User, ent))
            return;

        //run the same check again incase inventory changed during the doafter
        if (RecursivelyCheckForNesting(ent, skipInitialItem: true))
        {
            _popup.PopupClient(Loc.GetString("restrict-nesting-item-cant-pickup", ("user", ent)), args.User, args.User);
            return;
        }

        //if we get here, we can pickup the item
        //this is a forced pickup to fix dragging by species with no tails
        _handsSystem.TryForcePickupAnyHand(args.User, ent);
    }

    /// <summary>
    /// Recursively checks for nesting items in the inventory of the item passed in.
    /// </summary>
    /// <param name="item">
    /// What item to start checking at
    /// </param>
    /// <param name="depth">
    /// What depth the call is at. Should be left at 0 for external callers
    /// </param>
    /// <param name="skipInitialItem">
    /// Whether or not to check the initial item for the nesting component. Useful if a resomi is picking someone else up, to skip checking the first resomi
    /// </param>
    /// <returns></returns>
    protected bool RecursivelyCheckForNesting(EntityUid item, int depth = 0, bool skipInitialItem = false)
    {
        //check if we have hit the max depth, if so just as a safety return false.
        //This does mean if someone finds something that can possibly fit a resomi at our max depth,
        // they will be able to pick the person carrying the resomi up again,
        // but generally this should be mostly impossible due to the size of a resomi
        //only do this check if the initial item is a mob. This allows duffelbags to work

        if (depth > 20)
        {
            //log a warning
            Log.Warning($"{nameof(RecursivelyCheckForNesting)} hit max depth of {depth} for item {item}");
            return false;
        }
        
        if (skipInitialItem && !TryComp<MobMoverComponent>(item, out var mobMover))
            return false;

        //check if the item has the RestrictNestingItemComponent
        if (TryComp<RestrictNestingItemComponent>(item, out var nestingItem) && !skipInitialItem)
            return true;

        //get the container of the item
        if (!TryComp<ContainerManagerComponent>(item, out var containerManager))
            return false;

        //now run this on all items in the inventory
        var containers = containerManager.GetAllContainers().ToList();
        var items = containers.SelectMany(container => container.ContainedEntities).ToList();

        foreach (var itemInInventory in items)
        {
            //run recursive check
            if (RecursivelyCheckForNesting(itemInInventory, depth + 1))
            {
                return true;
            }
        }

        //if we get here, we have no nesting items in the inventory
        return false;
    }
}

//define the RestrictNestingItemPickupDoAfterEvent class
[Serializable, NetSerializable]
public sealed partial class RestrictNestingItemPickupDoAfterEvent : SimpleDoAfterEvent
{
}
