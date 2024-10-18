using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Server.Inventory;
using Content.Server.Nutrition.Components;
using Content.Shared.Nutrition.Components;
using Content.Server.Popups;
using Content.Server.Stack;
using Content.Shared.Administration.Logs;
using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Chemistry;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Stacks;
using Content.Shared.Storage;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Utility;
using System.Linq;
using Content.Shared.Containers.ItemSlots;
using Robust.Server.GameObjects;
using Content.Shared.Whitelist;
using Content.Shared.Destructible;

namespace Content.Server.Nutrition.EntitySystems;

/// <summary>
/// Handles feeding attempts both on yourself and on the target.
/// </summary>
public sealed class FoodSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly FlavorProfileSystem _flavorProfile = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly OpenableSystem _openable = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly ReactiveSystem _reaction = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly StomachSystem _stomach = default!;
    [Dependency] private readonly UtensilSystem _utensil = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    public const float MaxFeedDistance = 1.0f;

    public override void Initialize()
    {
        base.Initialize();

        // TODO add InteractNoHandEvent for entities like mice.
        // run after openable for wrapped/peelable foods
        SubscribeLocalEvent<FoodComponent, UseInHandEvent>(OnUseFoodInHand, after: new[] { typeof(OpenableSystem), typeof(ServerInventorySystem) });
        SubscribeLocalEvent<FoodComponent, AfterInteractEvent>(OnFeedFood);
        SubscribeLocalEvent<FoodComponent, GetVerbsEvent<AlternativeVerb>>(AddEatVerb);
        SubscribeLocalEvent<FoodComponent, ConsumeDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<InventoryComponent, IngestionAttemptEvent>(OnInventoryIngestAttempt);
    }

    /// <summary>
    /// Eat item
    /// </summary>
    private void OnUseFoodInHand(Entity<FoodComponent> entity, ref UseInHandEvent ev)
    {
        if (ev.Handled)
            return;

        var result = TryFeed(ev.User, ev.User, entity, entity.Comp);
        ev.Handled = result.Handled;
    }

    /// <summary>
    /// Feed someone else
    /// </summary>
    private void OnFeedFood(Entity<FoodComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target == null || !args.CanReach)
            return;

        var result = TryFeed(args.User, args.Target.Value, entity, entity.Comp);
        args.Handled = result.Handled;
    }

    /// <summary>
    /// Tries to feed the food item to the target entity
    /// </summary>
    public (bool Success, bool Handled) TryFeed(EntityUid user, EntityUid target, EntityUid food, FoodComponent foodComp)
    {
        //Suppresses eating yourself and alive mobs
        if (food == user || (_mobState.IsAlive(food) && foodComp.RequireDead))
            return (false, false);

        // Target can't be fed or they're already eating
        if (!TryComp<BodyComponent>(target, out var body))
            return (false, false);

        if (HasComp<UnremoveableComponent>(food))
            return (false, false);

        if (_openable.IsClosed(food, user))
            return (false, true);

        if (!_solutionContainer.TryGetSolution(food, foodComp.Solution, out _, out var foodSolution))
            return (false, false);

        if (!_body.TryGetBodyOrganEntityComps<StomachComponent>((target, body), out var stomachs))
            return (false, false);

        // Check for special digestibles
        if (!IsDigestibleBy(food, foodComp, stomachs))
            return (false, false);

        if (!TryGetRequiredUtensils(user, foodComp, out _))
            return (false, false);

        // Check for used storage on the food item
        if (TryComp<StorageComponent>(food, out var storageState) && storageState.Container.ContainedEntities.Any())
        {
            _popup.PopupEntity(Loc.GetString("food-has-used-storage", ("food", food)), user, user);
            return (false, true);
        }

        // Checks for used item slots
        if (TryComp<ItemSlotsComponent>(food, out var itemSlots))
        {
            if (itemSlots.Slots.Any(slot => slot.Value.HasItem))
            {
                _popup.PopupEntity(Loc.GetString("food-has-used-storage", ("food", food)), user, user);
                return (false, true);
            }
        }

        var flavors = _flavorProfile.GetLocalizedFlavorsMessage(food, user, foodSolution);

        if (GetUsesRemaining(food, foodComp) <= 0)
        {
            _popup.PopupEntity(Loc.GetString("food-system-try-use-food-is-empty", ("entity", food)), user, user);
            DeleteAndSpawnTrash(foodComp, food, user);
            return (false, true);
        }

        if (IsMouthBlocked(target, user))
            return (false, true);

        if (!_interaction.InRangeUnobstructed(user, food, popup: true))
            return (false, true);

        if (!_interaction.InRangeUnobstructed(user, target, MaxFeedDistance, popup: true))
            return (false, true);

        // TODO make do-afters account for fixtures in the range check.
        if (!_transform.GetMapCoordinates(user).InRange(_transform.GetMapCoordinates(target), MaxFeedDistance))
        {
            var message = Loc.GetString("interaction-system-user-interaction-cannot-reach");
            _popup.PopupEntity(message, user, user);
            return (false, true);
        }

        var forceFeed = user != target;
        if (forceFeed)
        {
            var userName = Identity.Entity(user, EntityManager);
            _popup.PopupEntity(Loc.GetString("food-system-force-feed", ("user", userName)),
                user, target);

            // logging
            _adminLogger.Add(LogType.ForceFeed, LogImpact.Medium, $"{ToPrettyString(user):user} is forcing {ToPrettyString(target):target} to eat {ToPrettyString(food):food} {SharedSolutionContainerSystem.ToPrettyString(foodSolution)}");
        }
        else
        {
            // log voluntary eating
            _adminLogger.Add(LogType.Ingestion, LogImpact.Low, $"{ToPrettyString(target):target} is eating {ToPrettyString(food):food} {SharedSolutionContainerSystem.ToPrettyString(foodSolution)}");
        }

        var doAfterArgs = new DoAfterArgs(EntityManager,
            user,
            forceFeed ? foodComp.ForceFeedDelay : foodComp.Delay,
            new ConsumeDoAfterEvent(foodComp.Solution, flavors),
            eventTarget: food,
            target: target,
            used: food)
        {
            BreakOnHandChange = false,
            BreakOnMove = forceFeed,
            BreakOnDamage = true,
            MovementThreshold = 0.01f,
            DistanceThreshold = MaxFeedDistance,
            // do-after will stop if item is dropped when trying to feed someone else
            // or if the item started out in the user's own hands
            NeedHand = forceFeed || _hands.IsHolding(user, food),
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        return (true, true);
    }

    private void OnDoAfter(Entity<FoodComponent> entity, ref ConsumeDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || entity.Comp.Deleted || args.Target == null)
            return;

        if (!TryComp<BodyComponent>(args.Target.Value, out var body))
            return;

        if (!_body.TryGetBodyOrganEntityComps<StomachComponent>((args.Target.Value, body), out var stomachs))
            return;

        if (args.Used is null || !_solutionContainer.TryGetSolution(args.Used.Value, args.Solution, out var soln, out var solution))
            return;

        if (!TryGetRequiredUtensils(args.User, entity.Comp, out var utensils))
            return;

        // TODO this should really be checked every tick.
        if (IsMouthBlocked(args.Target.Value))
            return;

        // TODO this should really be checked every tick.
        if (!_interaction.InRangeUnobstructed(args.User, args.Target.Value))
            return;

        var forceFeed = args.User != args.Target;

        args.Handled = true;
        var transferAmount = entity.Comp.TransferAmount != null ? FixedPoint2.Min((FixedPoint2) entity.Comp.TransferAmount, solution.Volume) : solution.Volume;

        var split = _solutionContainer.SplitSolution(soln.Value, transferAmount);

        // Get the stomach with the highest available solution volume
        var highestAvailable = FixedPoint2.Zero;
        Entity<StomachComponent>? stomachToUse = null;
        foreach (var ent in stomachs)
        {
            var owner = ent.Owner;
            if (!_stomach.CanTransferSolution(owner, split, ent.Comp1))
                continue;

            if (!_solutionContainer.ResolveSolution(owner, StomachSystem.DefaultSolutionName, ref ent.Comp1.Solution, out var stomachSol))
                continue;

            if (stomachSol.AvailableVolume <= highestAvailable)
                continue;

            stomachToUse = ent;
            highestAvailable = stomachSol.AvailableVolume;
        }

        // No stomach so just popup a message that they can't eat.
        if (stomachToUse == null)
        {
            _solutionContainer.TryAddSolution(soln.Value, split);
            _popup.PopupEntity(forceFeed ? Loc.GetString("food-system-you-cannot-eat-any-more-other") : Loc.GetString("food-system-you-cannot-eat-any-more"), args.Target.Value, args.User);
            return;
        }

        _reaction.DoEntityReaction(args.Target.Value, solution, ReactionMethod.Ingestion);
        _stomach.TryTransferSolution(stomachToUse!.Value.Owner, split, stomachToUse);

        var flavors = args.FlavorMessage;

        if (forceFeed)
        {
            var targetName = Identity.Entity(args.Target.Value, EntityManager);
            var userName = Identity.Entity(args.User, EntityManager);
            _popup.PopupEntity(Loc.GetString("food-system-force-feed-success", ("user", userName), ("flavors", flavors)), entity.Owner, entity.Owner);

            _popup.PopupEntity(Loc.GetString("food-system-force-feed-success-user", ("target", targetName)), args.User, args.User);

            // log successful force feed
            _adminLogger.Add(LogType.ForceFeed, LogImpact.Medium, $"{ToPrettyString(entity.Owner):user} forced {ToPrettyString(args.User):target} to eat {ToPrettyString(entity.Owner):food}");
        }
        else
        {
            _popup.PopupEntity(Loc.GetString(entity.Comp.EatMessage, ("food", entity.Owner), ("flavors", flavors)), args.User, args.User);

            // log successful voluntary eating
            _adminLogger.Add(LogType.Ingestion, LogImpact.Low, $"{ToPrettyString(args.User):target} ate {ToPrettyString(entity.Owner):food}");
        }

        _audio.PlayPvs(entity.Comp.UseSound, args.Target.Value, AudioParams.Default.WithVolume(-1f).WithVariation(0.20f));

        // Try to break all used utensils
        foreach (var utensil in utensils)
        {
            _utensil.TryBreak(utensil, args.User);
        }

        args.Repeat = !forceFeed;

        if (TryComp<StackComponent>(entity, out var stack))
        {
            //Not deleting whole stack piece will make troubles with grinding object
            if (stack.Count > 1)
            {
                _stack.SetCount(entity.Owner, stack.Count - 1);
                _solutionContainer.TryAddSolution(soln.Value, split);
                return;
            }
        }
        else if (GetUsesRemaining(entity.Owner, entity.Comp) > 0)
        {
            return;
        }

        // don't try to repeat if its being deleted
        args.Repeat = false;
        DeleteAndSpawnTrash(entity.Comp, entity.Owner, args.User);
    }

    public void DeleteAndSpawnTrash(FoodComponent component, EntityUid food, EntityUid user)
    {
        var ev = new BeforeFullyEatenEvent
        {
            User = user
        };
        RaiseLocalEvent(food, ev);
        if (ev.Cancelled)
            return;

        var dev = new DestructionEventArgs();
        RaiseLocalEvent(food, dev);

        if (component.Trash.Count == 0)
        {
            QueueDel(food);
            return;
        }

        //We're empty. Become trash.
        //cache some data as we remove food, before spawning trash and passing it to the hand.

        var position = _transform.GetMapCoordinates(food);
        var trashes = component.Trash;
        var tryPickup = _hands.IsHolding(user, food, out _);

        Del(food);
        foreach (var trash in trashes)
        {
            var spawnedTrash = Spawn(trash, position);

            // If the user is holding the item
            if (tryPickup)
            {
                // Put the trash in the user's hand
                _hands.TryPickupAnyHand(user, spawnedTrash);
            }
        }
    }

    private void AddEatVerb(Entity<FoodComponent> entity, ref GetVerbsEvent<AlternativeVerb> ev)
    {
        if (entity.Owner == ev.User ||
            !ev.CanInteract ||
            !ev.CanAccess ||
            !TryComp<BodyComponent>(ev.User, out var body) ||
            !_body.TryGetBodyOrganEntityComps<StomachComponent>((ev.User, body), out var stomachs))
            return;

        // have to kill mouse before eating it
        if (_mobState.IsAlive(entity) && entity.Comp.RequireDead)
            return;

        // only give moths eat verb for clothes since it would just fail otherwise
        if (!IsDigestibleBy(entity, entity.Comp, stomachs))
            return;

        var user = ev.User;
        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                TryFeed(user, user, entity, entity.Comp);
            },
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/cutlery.svg.192dpi.png")),
            Text = Loc.GetString("food-system-verb-eat"),
            Priority = -1
        };

        ev.Verbs.Add(verb);
    }

    /// <summary>
    ///     Returns true if the food item can be digested by the user.
    /// </summary>
    public bool IsDigestibleBy(EntityUid uid, EntityUid food, FoodComponent? foodComp = null)
    {
        if (!Resolve(food, ref foodComp, false))
            return false;

        if (!_body.TryGetBodyOrganEntityComps<StomachComponent>(uid, out var stomachs))
            return false;

        return IsDigestibleBy(food, foodComp, stomachs);
    }

    /// <summary>
    ///     Returns true if <paramref name="stomachs"/> has a <see cref="StomachComponent.SpecialDigestible"/> that whitelists
    ///     this <paramref name="food"/> (or if they even have enough stomachs in the first place).
    /// </summary>
    private bool IsDigestibleBy(EntityUid food, FoodComponent component, List<Entity<StomachComponent, OrganComponent>> stomachs)
    {
        var digestible = true;

        // Does the mob have enough stomachs?
        if (stomachs.Count < component.RequiredStomachs)
            return false;

        // Run through the mobs' stomachs
        foreach (var ent in stomachs)
        {
            // Find a stomach with a SpecialDigestible
            if (ent.Comp1.SpecialDigestible == null)
                continue;
            // Check if the food is in the whitelist
            if (_whitelistSystem.IsWhitelistPass(ent.Comp1.SpecialDigestible, food))
                return true;
            // They can only eat whitelist food and the food isn't in the whitelist. It's not edible.
            return false;
        }

        if (component.RequiresSpecialDigestion)
            return false;

        return digestible;
    }

    private bool TryGetRequiredUtensils(EntityUid user, FoodComponent component,
        out List<EntityUid> utensils, HandsComponent? hands = null)
    {
        utensils = new List<EntityUid>();

        if (component.Utensil == UtensilType.None)
            return true;

        if (!Resolve(user, ref hands, false))
            return true; //mice

        var usedTypes = UtensilType.None;

        foreach (var item in _hands.EnumerateHeld(user, hands))
        {
            // Is utensil?
            if (!TryComp<UtensilComponent>(item, out var utensil))
                continue;

            if ((utensil.Types & component.Utensil) != 0 && // Acceptable type?
                (usedTypes & utensil.Types) != utensil.Types) // Type is not used already? (removes usage of identical utensils)
            {
                // Add to used list
                usedTypes |= utensil.Types;
                utensils.Add(item);
            }
        }

        // If "required" field is set, try to block eating without proper utensils used
        if (component.UtensilRequired && (usedTypes & component.Utensil) != component.Utensil)
        {
            _popup.PopupEntity(Loc.GetString("food-you-need-to-hold-utensil", ("utensil", component.Utensil ^ usedTypes)), user, user);
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Block ingestion attempts based on the equipped mask or head-wear
    /// </summary>
    private void OnInventoryIngestAttempt(Entity<InventoryComponent> entity, ref IngestionAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        IngestionBlockerComponent? blocker;

        if (_inventory.TryGetSlotEntity(entity.Owner, "mask", out var maskUid) &&
            TryComp(maskUid, out blocker) &&
            blocker.Enabled)
        {
            args.Blocker = maskUid;
            args.Cancel();
            return;
        }

        if (_inventory.TryGetSlotEntity(entity.Owner, "head", out var headUid) &&
            TryComp(headUid, out blocker) &&
            blocker.Enabled)
        {
            args.Blocker = headUid;
            args.Cancel();
        }
    }


    /// <summary>
    ///     Check whether the target's mouth is blocked by equipment (masks or head-wear).
    /// </summary>
    /// <param name="uid">The target whose equipment is checked</param>
    /// <param name="popupUid">Optional entity that will receive an informative pop-up identifying the blocking
    /// piece of equipment.</param>
    /// <returns></returns>
    public bool IsMouthBlocked(EntityUid uid, EntityUid? popupUid = null)
    {
        var attempt = new IngestionAttemptEvent();
        RaiseLocalEvent(uid, attempt, false);
        if (attempt.Cancelled && attempt.Blocker != null && popupUid != null)
        {
            _popup.PopupEntity(Loc.GetString("food-system-remove-mask", ("entity", attempt.Blocker.Value)),
                uid, popupUid.Value);
        }

        return attempt.Cancelled;
    }

    /// <summary>
    /// Get the number of bites this food has left, based on how much food solution there is and how much of it to eat per bite.
    /// </summary>
    public int GetUsesRemaining(EntityUid uid, FoodComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return 0;

        if (!_solutionContainer.TryGetSolution(uid, comp.Solution, out _, out var solution) || solution.Volume == 0)
            return 0;

        // eat all in 1 go, so non empty is 1 bite
        if (comp.TransferAmount == null)
            return 1;

        return Math.Max(1, (int) Math.Ceiling((solution.Volume / (FixedPoint2) comp.TransferAmount).Float()));
    }
}
