using System.Diagnostics.CodeAnalysis;
using Content.Shared.Administration.Logs;
using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;

namespace Content.Shared.Nutrition.EntitySystems;

/// <summary>
/// This handles the ingestion of solutions.
/// </summary>
public sealed partial class IngestionSystem : EntitySystem
{
    public const float MaxFeedDistance = 1.0f; // We should really have generic interaction ranges like short, medium, long and use those instead...
    // BodySystem has no way of telling us where the mouth is so we're making some assumptions.
    public const SlotFlags DefaultFlags = SlotFlags.HEAD | SlotFlags.MASK;

    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly FlavorProfileSystem _flavorProfile = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    // Body Component Dependencies
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly ReactiveSystem _reaction = default!;
    [Dependency] private readonly StomachSystem _stomach = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        // Interactions
        SubscribeLocalEvent<EdibleComponent, UseInHandEvent>(OnUseFoodInHand, after: new[] { typeof(OpenableSystem), typeof(InventorySystem) });
        SubscribeLocalEvent<EdibleComponent, AfterInteractEvent>(OnFeedFood);

        // Generic Eating Handlers
        SubscribeLocalEvent<EdibleComponent, EatenEvent>(OnEdibleEaten);

        // Body Component eating handler
        SubscribeLocalEvent<BodyComponent, CanIngestEvent>(OnTryIngest);
        SubscribeLocalEvent<BodyComponent, EatingDoAfterEvent>(OnEatingDoAfter);

        InitializeBlockers();
        InitializeTypes();
        InitializeUtensils();
    }

    /// <summary>
    /// Eat or drink an item
    /// </summary>
    private void OnUseFoodInHand(Entity<EdibleComponent> entity, ref UseInHandEvent ev)
    {
        if (ev.Handled)
            return;

        ev.Handled = TryIngest(ev.User, ev.User, entity);
    }

    /// <summary>
    /// Feed someone else
    /// </summary>
    private void OnFeedFood(Entity<EdibleComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target == null || !args.CanReach)
            return;

        args.Handled = TryIngest(args.User, args.Target.Value, entity);
    }

    /// <summary>
    /// Tries to make an entity ingest another entity.
    /// </summary>
    /// <param name="user">The entity who is trying to make this happen.</param>
    /// <param name="target">The entity who is being made to ingest something.</param>
    /// <param name="ingested">The entity that is trying to be ingested.</param>
    /// <param name="solution">The solution which we may or may not be taking reagents from</param>
    /// <param name="ingest">Whether we're actually ingesting the item or if we're just testing.</param>
    /// <returns>Returns true if we can ingest the item.</returns>
    public bool TryIngest(EntityUid user, EntityUid target, EntityUid ingested, bool ingest = true)
    {
        var eatEv = new IngestibleEvent();
        RaiseLocalEvent(ingested, ref eatEv);

        if (eatEv.Cancelled)
            return false;

        var ingestionEv = new CanIngestEvent
        {
            User = user,
            Ingested = ingested,
            Ingest = ingest
        };
        RaiseLocalEvent(target, ref ingestionEv);

        return ingestionEv.Handled;
    }

    /// <summary>
    ///     Check whether we have an open pie-hole that's in range.
    /// </summary>
    /// <param name="user">The one performing the action</param>
    /// <param name="target">The target whose mouth is checked</param>
    /// <param name="popupUid">Optional entity that will receive an informative pop-up identifying the blocking
    /// piece of equipment.</param>
    /// <returns></returns>
    public bool HasMouthAvailable(EntityUid user, EntityUid target, EntityUid? popupUid = null)
    {
        return HasMouthAvailable(user, target, DefaultFlags, popupUid);
    }

    /// <inheritdoc cref="HasMouthAvailable(EntityUid, EntityUid, EntityUid?)"/>
    /// Overflow which takes custom flags for a mouth being blocked, in case the entity has a mouth not on the face.
    public bool HasMouthAvailable(EntityUid user, EntityUid target, SlotFlags flags, EntityUid? popupUid = null)
    {
        if (!_transform.GetMapCoordinates(user).InRange(_transform.GetMapCoordinates(target), MaxFeedDistance))
        {
            var message = Loc.GetString("interaction-system-user-interaction-cannot-reach");
            _popup.PopupClient(message, user, user);
            return false;
        }

        var attempt = new IngestionAttemptEvent
        {
            TargetSlots = flags,
        };
        RaiseLocalEvent(target, ref attempt);

        if (!attempt.Cancelled)
            return true;

        if (attempt.Blocker != null && popupUid != null)
            _popup.PopupClient(Loc.GetString("food-system-remove-mask", ("entity", attempt.Blocker.Value)), user, popupUid.Value);

        return false;
    }

    /// <summary>
    ///     Tries to feed a nutrition solution to a target
    /// </summary>
    /// <param name="user">The one doing the feeding</param>
    /// <param name="target">The one being fed.</param>
    /// <param name="ingested">The food item being eaten.</param>
    /// <returns></returns>
    private bool CanFeed(EntityUid user, EntityUid target, Entity<EdibleComponent?> ingested)
    {
        if (!Resolve(ingested, ref ingested.Comp, false))
            return false;

        if (!HasMouthAvailable(target, user))
            return false;

        // If we don't have the tools to eat we can't eat.
        // TODO: Do this somewhere else...
        return TryAccessSolution(ingested!, user);
    }

    /// <summary>
    /// Checks if we can get the solution from the entity we're trying to eat given our method of eating.
    /// Doesn't return the solution
    /// </summary>
    private bool TryAccessSolution(Entity<EdibleComponent> ingested, EntityUid user)
    {
        return TryAccessSolution(ingested!, user, out _);
    }

    /// <summary>
    /// Tries to access the solution which we are trying to eat.
    /// Yummy. TODO: DESCRIPTION
    /// </summary>
    private bool TryAccessSolution(Entity<EdibleComponent?, SolutionContainerManagerComponent?> ingested, EntityUid user,  [NotNullWhen(true)] out Entity<SolutionComponent>? solution)
    {
        solution = null;

        if (!Resolve(ingested, ref ingested.Comp1, ref ingested.Comp2))
            return false;

        var ev = new EdibleEvent
        {
            User = user,
            Destroy = ingested.Comp1.DeleteOnEmpty
        };
        RaiseLocalEvent(ingested, ref ev);

        if (ev.Cancelled)
            return false;

        // TODO: I fucking hate utensils so fucking much man
        // TODO: Need to return utensils somewhere so we can try and break them.
        if (ingested.Comp1.UtensilRequired && TryGetRequiredUtensils(user, ingested.Comp1, out _))
            return false;

        // Actually try and get the solution we're looking for.
        return _solutionContainer.TryGetSolution(ingested.Owner, ingested.Comp1.Solution, out solution);
    }

    /// <summary>
    ///     Returns true if <paramref name="stomachs"/> has a <see cref="StomachComponent.SpecialDigestible"/> that whitelists
    ///     this <paramref name="food"/> (or if they even have enough stomachs in the first place).
    /// </summary>
    private bool IsDigestibleBy(Entity<EdibleComponent?> food, List<Entity<StomachComponent, OrganComponent>> stomachs)
    {
        if (!Resolve(food, ref food.Comp, false))
            return false;

        if (food.Comp.RequireDead && _mobState.IsAlive(food))
            return false;

        if (food.Comp.RequiresSpecialDigestion)
        {
            foreach (var ent in stomachs)
            {
                // We need one stomach that can digest our special food.
                if (ent.Comp1.SpecialDigestible != null
                    && _whitelistSystem.IsWhitelistPass(ent.Comp1.SpecialDigestible, food))
                    return true;
            }
        }
        else
        {
            foreach (var ent in stomachs)
            {
                // We need one stomach that can digest normal food.
                if (ent.Comp1.SpecialDigestible == null
                    || !ent.Comp1.IsSpecialDigestibleExclusive)
                    return true;
            }
        }

        // If we didn't find a stomach that can digest our food then it doesn't exist.
        return false;
    }

    /// <summary>
    /// Overflow method which takes a single stomach into account.
    /// </summary>
    private bool IsDigestibleBy(Entity<EdibleComponent?> food, Entity<StomachComponent, OrganComponent> stomach)
    {
        if (!Resolve(food, ref food.Comp, false))
            return false;

        if (food.Comp.RequiresSpecialDigestion &&
            stomach.Comp1.SpecialDigestible != null
            && _whitelistSystem.IsWhitelistPass(stomach.Comp1.SpecialDigestible, food))
            return true;

        if (stomach.Comp1.SpecialDigestible == null || !stomach.Comp1.IsSpecialDigestibleExclusive)
            return true;

        return false;
    }

    #region BodySystem Handlers

    private void OnTryIngest(Entity<BodyComponent> entity, ref CanIngestEvent args)
    {
        var food = args.Ingested;

        if (!Resolve(food, ref food.Comp, false))
            return;

        if (!_body.TryGetBodyOrganEntityComps<StomachComponent>(entity!, out var stomachs))
            return;

        // Can we digest the specific item we're trying to eat?
        if (!IsDigestibleBy(args.Ingested, stomachs))
            return;

        // Exit early if we're just trying to get verbs
        if (!args.Ingest)
        {
            args.Handled = true;
            return;
        }

        // Check if despite being able to digest the item something is blocking us from eating.
        if (!CanFeed(args.User, entity, args.Ingested))
            return;

        args.Handled = _doAfter.TryStartDoAfter(GetEatingDoAfterArgs(args.User, entity, food!));
        // TODO: Admin logs here ;_;
    }

    private void OnEatingDoAfter(Entity<BodyComponent> entity, ref EatingDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || entity.Comp.Deleted || args.Target == null)
            return;

        var food = args.Target.Value;

        if (!CanFeed(args.User, entity, food))
            return;

        if (!_body.TryGetBodyOrganEntityComps<StomachComponent>(entity!, out var stomachs))
            return;

        // Can we digest the specific item we're trying to eat?
        if (!IsDigestibleBy(food, stomachs))
            return;

        // Tell the food something is eating it so it can return a solution.
        var eatenEv = new EatenEvent
        {
            User = args.User,
        };
        RaiseLocalEvent(food, ref eatenEv);

        if (!TryAccessSolution(food, args.User, out var solution))
            return;

        var forceFeed = args.User != entity.Owner;

        // TODO: Everything below this needs to be rewritten (And maybe everything above moved to its own method).

        // DO NOT FUCKING TRYCOMP EDIBLE???


        //if (!TryComp<EdibleComponent>(food, out var edible) || !TryAccessSolution((food, edible), args.User, out var solution))
            //return;

        // TODO: This wont work with the new stomach selector, it needs to select the best available stomach with the highest solution remaining.
        // Get the stomach with the highest available solution volume
        var highestAvailable = FixedPoint2.Zero;
        Entity<StomachComponent>? stomachToUse = null;
        foreach (var ent in stomachs)
        {
            // TODO: Get max available volume instead and if it's 0 return full.
            var owner = ent.Owner;
            if (!_solutionContainer.ResolveSolution(owner, StomachSystem.DefaultSolutionName, ref ent.Comp1.Solution, out var stomachSol))
                continue;

            if (stomachSol.AvailableVolume <= highestAvailable)
                continue;

            if (!IsDigestibleBy(food, ent))
                continue;

            stomachToUse = ent;
            highestAvailable = stomachSol.AvailableVolume;
        }

        // All stomachs are full
        if (stomachToUse == null)
        {
            _popup.PopupClient(forceFeed ? Loc.GetString("food-system-you-cannot-eat-any-more-other", ("target", args.Target.Value)) : Loc.GetString("food-system-you-cannot-eat-any-more"), args.Target.Value, args.User);
            return;
        }

        // TODO: Get Transfer volume
        var beforeEv = new BeforeEatenEvent
        {
            User = args.User,
        };
        RaiseLocalEvent(food, ref beforeEv);

        var split = _solutionContainer.SplitSolution(solution.Value, FixedPoint2.Min(highestAvailable, FixedPoint2.Zero));

        // TODO: FUCK
        _reaction.DoEntityReaction(args.Target.Value, split, ReactionMethod.Ingestion);
        _stomach.TryTransferSolution(stomachToUse.Value.Owner, split, stomachToUse);

        // Everything is good to go item has been successfuly eaten
        var afterEv = new EatenEvent
        {
            User = args.User,
            Target = args.Target.Value,
        };
        RaiseLocalEvent(food, ref afterEv);

        // TODO: I need this to be an event
        // TODO: This also deletes reagents if the item being stacked takes more than one bite to eat because of fucking course it does why wouldn't it???
        // TODO: I need this to be two events
        if (TryComp<StackComponent>(entity, out var stack))
        {
            //Not deleting whole stack piece will make troubles with grinding object
            if (stack.Count > 1)
            {
                _stack.SetCount(entity.Owner, stack.Count - 1);
                _solutionContainer.TryAddSolution(solution.Value, split);
                return;
            }
        }
        else if (GetUsesRemaining(food) > 0)
        {
            return;
        }

        // don't try to repeat if its being deleted
        args.Repeat = false;
        // TODO: MOVE THIS TO ONE OF THE EDIBLE EVENT HANDLERS OR SOMETHING
        //DeleteAndSpawnTrash((food, edible), args.User);
    }

    // TODO: Just get shit working first then make it all nice later
    /// <summary>
    /// Gets the DoAfterArgs for the specific event
    /// </summary>
    /// <param name="user"></param>
    /// <param name="target"></param>
    /// <param name="food"></param>
    /// <returns></returns>
    private DoAfterArgs GetEatingDoAfterArgs(EntityUid user, EntityUid target, Entity<EdibleComponent> food)
    {
        var forceFeed = user != target;

        var doAfterArgs = new DoAfterArgs(EntityManager, user, food.Comp.Delay, new EatingDoAfterEvent(), target, food)
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

        return doAfterArgs;
    }

    #endregion

    /// <summary>
    /// Get the number of bites this food has left, based on how much food solution there is and how much of it to eat per bite.
    /// OUUUGH
    /// </summary>
    public int GetUsesRemaining(Entity<EdibleComponent?> entity)
    {
        // TODO: NOT MY CODE
        if (!Resolve(entity, ref entity.Comp))
            return 0;

        if (!_solutionContainer.TryGetSolution(entity.Owner, entity.Comp.Solution, out _, out var solution) || solution.Volume == 0)
            return 0;

        // eat all in 1 go, so non empty is 1 bite
        if (entity.Comp.TransferAmount == null)
            return 1;

        return Math.Max(1, (int) Math.Ceiling((solution.Volume / (FixedPoint2) entity.Comp.TransferAmount).Float()));
    }

    public void DeleteAndSpawnTrash(Entity<EdibleComponent> food, EntityUid user)
    {
        // TODO: USE DESTRUCTION SYSTEM AND EVENTS FOR THIS
        // TODO: REITERATE THAT POINT
        var ev = new BeforeFullyEatenEvent
        {
            User = user
        };
        RaiseLocalEvent(food, ev);
        if (ev.Cancelled)
            return;

        var attemptEv = new DestructionAttemptEvent();
        RaiseLocalEvent(food, attemptEv);
        if (attemptEv.Cancelled)
            return;

        var afterEvent = new AfterFullyEatenEvent(user);
        RaiseLocalEvent(food, ref afterEvent);

        var dev = new DestructionEventArgs();
        RaiseLocalEvent(food, dev);

        if (food.Comp.Trash.Count == 0)
        {
            PredictedQueueDel(food.Owner);
            return;
        }

        //We're empty. Become trash.
        //cache some data as we remove food, before spawning trash and passing it to the hand.

        var position = _transform.GetMapCoordinates(food);
        var trashes = food.Comp.Trash;
        var tryPickup = _hands.IsHolding(user, food, out _);

        PredictedDel(food.Owner);
        foreach (var trash in trashes)
        {
            var spawnedTrash = EntityManager.PredictedSpawn(trash, position);

            // If the user is holding the item
            if (tryPickup)
            {
                // Put the trash in the user's hand
                _hands.TryPickupAnyHand(user, spawnedTrash);
            }
        }
    }

    private void OnEdibleEaten(Entity<EdibleComponent> entity, ref EatenEvent args)
    {
        // TODO: I NEED THE FUCKING SPLIT!?!?!
        // TODO: THIS IS SHIT!!!!
        var split = new Solution();

        var flavors = _flavorProfile.GetLocalizedFlavorsMessage(entity, args.User, split);

        if (args.User != args.Target)
        {
            var targetName = Identity.Entity(args.Target, EntityManager);
            var userName = Identity.Entity(args.User, EntityManager);
            _popup.PopupEntity(Loc.GetString("food-system-force-feed-success", ("user", userName), ("flavors", flavors)), entity.Owner, entity.Owner);

            _popup.PopupClient(Loc.GetString("food-system-force-feed-success-user", ("target", targetName)), args.User, args.User);

            // log successful force feed
            _adminLogger.Add(LogType.ForceFeed, LogImpact.Medium, $"{ToPrettyString(entity.Owner):user} forced {ToPrettyString(args.User):target} to eat {ToPrettyString(entity.Owner):food}");
        }
        else
        {
            _popup.PopupClient(Loc.GetString(entity.Comp.EatMessage, ("food", entity.Owner), ("flavors", flavors)), args.User, args.User);

            // log successful voluntary eating
            _adminLogger.Add(LogType.Ingestion, LogImpact.Low, $"{ToPrettyString(args.User):target} ate {ToPrettyString(entity.Owner):food}");
        }

        _audio.PlayPredicted(entity.Comp.UseSound, args.Target, args.User, AudioParams.Default.WithVolume(-1f).WithVariation(0.20f));

        // TODO: Utensils

        // TODO: RETURN DO-AFTER ARGS
        //args.Repeat = !forceFeed;

        args.Destroy = entity.Comp.DeleteOnEmpty; //&& We're empty. TODO: Maybe don't have this here...
    }
}
