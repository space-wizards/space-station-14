using System.Linq;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Inventory;
using Content.Server.Nutrition.Components;
using Content.Server.Popups;
using Content.Server.Stack;
using Content.Shared.Administration.Logs;
using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition;
using Content.Shared.Verbs;
using Content.Shared.Stacks;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using Content.Shared.Tag;
using Content.Shared.Storage;

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
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly StomachSystem _stomach = default!;
    [Dependency] private readonly UtensilSystem _utensil = default!;

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
    private void OnUseFoodInHand(EntityUid uid, FoodComponent foodComponent, UseInHandEvent ev)
    {
        if (ev.Handled)
            return;

        var result = TryFeed(ev.User, ev.User, uid, foodComponent);
        ev.Handled = result.Handled;
    }

    /// <summary>
    /// Feed someone else
    /// </summary>
    private void OnFeedFood(EntityUid uid, FoodComponent foodComponent, AfterInteractEvent args)
    {
        if (args.Handled || args.Target == null || !args.CanReach)
            return;

        var result = TryFeed(args.User, args.Target.Value, uid, foodComponent);
        args.Handled = result.Handled;
    }

    public (bool Success, bool Handled) TryFeed(EntityUid user, EntityUid target, EntityUid food, FoodComponent foodComp)
    {
        //Suppresses eating yourself and alive mobs
        if (food == user || _mobState.IsAlive(food))
            return (false, false);

        // Target can't be fed or they're already eating
        if (!TryComp<BodyComponent>(target, out var body))
            return (false, false);

        if (_openable.IsClosed(food, user))
            return (false, true);

        if (!_solutionContainer.TryGetSolution(food, foodComp.Solution, out var foodSolution) || foodSolution.Name == null)
            return (false, false);

        if (!_body.TryGetBodyOrganComponents<StomachComponent>(target, out var stomachs, body))
            return (false, false);

        // Check for special digestibles
        if (!IsDigestibleBy(food, foodComp, stomachs))
            return (false, false);

        if (!TryGetRequiredUtensils(user, foodComp, out _))
            return (false, false);

        // Check for used storage on the food item
        if (TryComp<StorageComponent>(food, out var storageState) && storageState.StorageUsed != 0)
        {
            _popup.PopupEntity(Loc.GetString("food-has-used-storage", ("food", food)), user, user);
            return (false, true);
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
        if (!Transform(user).MapPosition.InRange(Transform(target).MapPosition, MaxFeedDistance))
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
            _adminLogger.Add(LogType.ForceFeed, LogImpact.Medium, $"{ToPrettyString(user):user} is forcing {ToPrettyString(target):target} to eat {ToPrettyString(food):food} {SolutionContainerSystem.ToPrettyString(foodSolution)}");
        }
        else
        {
            // log voluntary eating
            _adminLogger.Add(LogType.Ingestion, LogImpact.Low, $"{ToPrettyString(target):target} is eating {ToPrettyString(food):food} {SolutionContainerSystem.ToPrettyString(foodSolution)}");
        }

        var doAfterArgs = new DoAfterArgs(EntityManager,
            user,
            forceFeed ? foodComp.ForceFeedDelay : foodComp.Delay,
            new ConsumeDoAfterEvent(foodSolution.Name, flavors),
            eventTarget: food,
            target: target,
            used: food)
        {
            BreakOnUserMove = forceFeed,
            BreakOnDamage = true,
            BreakOnTargetMove = forceFeed,
            MovementThreshold = 0.01f,
            DistanceThreshold = MaxFeedDistance,
            // Mice and the like can eat without hands.
            // TODO maybe set this based on some CanEatWithoutHands event or component?
            NeedHand = forceFeed,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        return (true, true);
    }

    private void OnDoAfter(EntityUid uid, FoodComponent component, ConsumeDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || component.Deleted || args.Target == null)
            return;

        if (!TryComp<BodyComponent>(args.Target.Value, out var body))
            return;

        if (!_body.TryGetBodyOrganComponents<StomachComponent>(args.Target.Value, out var stomachs, body))
            return;

        if (!_solutionContainer.TryGetSolution(args.Used, args.Solution, out var solution))
            return;

        if (!TryGetRequiredUtensils(args.User, component, out var utensils))
            return;

        // TODO this should really be checked every tick.
        if (IsMouthBlocked(args.Target.Value))
            return;

        // TODO this should really be checked every tick.
        if (!_interaction.InRangeUnobstructed(args.User, args.Target.Value))
            return;

        var forceFeed = args.User != args.Target;

        args.Handled = true;
        var transferAmount = component.TransferAmount != null ? FixedPoint2.Min((FixedPoint2) component.TransferAmount, solution.Volume) : solution.Volume;

        var split = _solutionContainer.SplitSolution(uid, solution, transferAmount);

        //TODO: Get the stomach UID somehow without nabbing owner
        // Get the stomach with the highest available solution volume
        var highestAvailable = FixedPoint2.Zero;
        StomachComponent? stomachToUse = null;
        foreach (var (stomach, _) in stomachs)
        {
            var owner = stomach.Owner;
            if (!_stomach.CanTransferSolution(owner, split))
                continue;

            if (!_solutionContainer.TryGetSolution(owner, StomachSystem.DefaultSolutionName,
                    out var stomachSol))
                continue;

            if (stomachSol.AvailableVolume <= highestAvailable)
                continue;

            stomachToUse = stomach;
            highestAvailable = stomachSol.AvailableVolume;
        }

        // No stomach so just popup a message that they can't eat.
        if (stomachToUse == null)
        {
            _solutionContainer.TryAddSolution(uid, solution, split);
            _popup.PopupEntity(forceFeed ? Loc.GetString("food-system-you-cannot-eat-any-more-other") : Loc.GetString("food-system-you-cannot-eat-any-more"), args.Target.Value, args.User);
            return;
        }

        _reaction.DoEntityReaction(args.Target.Value, solution, ReactionMethod.Ingestion);
        _stomach.TryTransferSolution(stomachToUse.Owner, split, stomachToUse);

        var flavors = args.FlavorMessage;

        if (forceFeed)
        {
            var targetName = Identity.Entity(args.Target.Value, EntityManager);
            var userName = Identity.Entity(args.User, EntityManager);
            _popup.PopupEntity(Loc.GetString("food-system-force-feed-success", ("user", userName), ("flavors", flavors)),
                uid, uid);

            _popup.PopupEntity(Loc.GetString("food-system-force-feed-success-user", ("target", targetName)), args.User, args.User);

            // log successful force feed
            _adminLogger.Add(LogType.ForceFeed, LogImpact.Medium, $"{ToPrettyString(uid):user} forced {ToPrettyString(args.User):target} to eat {ToPrettyString(uid):food}");
        }
        else
        {
            _popup.PopupEntity(Loc.GetString(component.EatMessage, ("food", uid), ("flavors", flavors)), args.User, args.User);

            // log successful voluntary eating
            _adminLogger.Add(LogType.Ingestion, LogImpact.Low, $"{ToPrettyString(args.User):target} ate {ToPrettyString(uid):food}");
        }

        _audio.Play(component.UseSound, Filter.Pvs(args.Target.Value), args.Target.Value, true, AudioParams.Default.WithVolume(-1f));

        // Try to break all used utensils
        foreach (var utensil in utensils)
        {
            _utensil.TryBreak(utensil, args.User);
        }

        args.Repeat = !forceFeed;

        if (TryComp<StackComponent>(uid, out var stack))
        {
            //Not deleting whole stack piece will make troubles with grinding object
            if (stack.Count > 1)
            {
                _stack.SetCount(uid, stack.Count - 1);
                _solutionContainer.TryAddSolution(uid, solution, split);
                return;
            }
        }
        else if (GetUsesRemaining(uid, component) > 0)
        {
            return;
        }

        var ev = new BeforeFullyEatenEvent
        {
            User = args.User
        };
        RaiseLocalEvent(uid, ev);
        if (ev.Cancelled)
            return;

        if (string.IsNullOrEmpty(component.TrashPrototype))
            QueueDel(uid);
        else
            DeleteAndSpawnTrash(component, uid, args.User);
    }

    public void DeleteAndSpawnTrash(FoodComponent component, EntityUid food, EntityUid? user = null)
    {
        //We're empty. Become trash.
        var position = Transform(food).MapPosition;
        var finisher = Spawn(component.TrashPrototype, position);

        // If the user is holding the item
        if (user != null && _hands.IsHolding(user.Value, food, out var hand))
        {
            Del(food);

            // Put the trash in the user's hand
            _hands.TryPickup(user.Value, finisher, hand);
            return;
        }

        QueueDel(food);
    }

    private void AddEatVerb(EntityUid uid, FoodComponent component, GetVerbsEvent<AlternativeVerb> ev)
    {
        if (uid == ev.User ||
            !ev.CanInteract ||
            !ev.CanAccess ||
            !TryComp<BodyComponent>(ev.User, out var body) ||
            !_body.TryGetBodyOrganComponents<StomachComponent>(ev.User, out var stomachs, body))
            return;

        // have to kill mouse before eating it
        if (_mobState.IsAlive(uid))
            return;

        // only give moths eat verb for clothes since it would just fail otherwise
        if (!IsDigestibleBy(uid, component, stomachs))
            return;

        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                TryFeed(ev.User, ev.User, uid, component);
            },
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/cutlery.svg.192dpi.png")),
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

        if (!_body.TryGetBodyOrganComponents<StomachComponent>(uid, out var stomachs))
            return false;

        return IsDigestibleBy(food, foodComp, stomachs);
    }

    /// <summary>
    ///     Returns true if <paramref name="stomachs"/> has a <see cref="StomachComponent.SpecialDigestible"/> that whitelists
    ///     this <paramref name="food"/> (or if they even have enough stomachs in the first place).
    /// </summary>
    private bool IsDigestibleBy(EntityUid food, FoodComponent component, List<(StomachComponent, OrganComponent)> stomachs)
    {
        var digestible = true;

        // Does the mob have enough stomachs?
        if (stomachs.Count < component.RequiredStomachs)
            return false;

        // Run through the mobs' stomachs
        foreach (var (comp, _) in stomachs)
        {
            // Find a stomach with a SpecialDigestible
            if (comp.SpecialDigestible == null)
                continue;
            // Check if the food is in the whitelist
            if (comp.SpecialDigestible.IsValid(food, EntityManager))
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

        if (component.Utensil != UtensilType.None)
            return true;

        if (!Resolve(user, ref hands, false))
            return false;

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
    private void OnInventoryIngestAttempt(EntityUid uid, InventoryComponent component, IngestionAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        IngestionBlockerComponent? blocker;

        if (_inventory.TryGetSlotEntity(uid, "mask", out var maskUid) &&
            TryComp(maskUid, out blocker) &&
            blocker.Enabled)
        {
            args.Blocker = maskUid;
            args.Cancel();
            return;
        }

        if (_inventory.TryGetSlotEntity(uid, "head", out var headUid) &&
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

        if (!_solutionContainer.TryGetSolution(uid, comp.Solution, out var solution) || solution.Volume == 0)
            return 0;

        // eat all in 1 go, so non empty is 1 bite
        if (comp.TransferAmount == null)
            return 1;

        return Math.Max(1, (int) Math.Ceiling((solution.Volume / (FixedPoint2) comp.TransferAmount).Float()));
    }
}
