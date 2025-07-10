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

        ev.Handled = CanFeed(ev.User, ev.User, entity!);
    }

    /// <summary>
    /// Feed someone else
    /// </summary>
    private void OnFeedFood(Entity<EdibleComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target == null || !args.CanReach)
            return;

        args.Handled = CanFeed(args.User, args.Target.Value, entity!);
    }

    public bool TryIngest(EntityUid user, EntityUid target, EntityUid ingested, bool ingest = true)
    {
        return TryIngest(user, target, ingested, null, ingest);
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
    public bool TryIngest(EntityUid user, EntityUid target, EntityUid ingested, Entity<SolutionComponent>? solution, bool ingest = true)
    {
        var eatEv = new IngestibleEvent();
        RaiseLocalEvent(ingested, ref eatEv);

        if (eatEv.Cancelled)
            return false;

        // Exit early if we're not making a new ingestion attempt
        if (!ingest)
            return true;

        var ingestionEv = new CanIngestEvent()
        {
            User = user,
            Ingested = ingested,
            Solution = null,
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
        if (!_transform.GetMapCoordinates(user).InRange(_transform.GetMapCoordinates(target), MaxFeedDistance))
        {
            var message = Loc.GetString("interaction-system-user-interaction-cannot-reach");
            _popup.PopupClient(message, user, user);
            return false;
        }

        var attempt = new IngestionAttemptEvent();
        RaiseLocalEvent(target, ref attempt);

        if (!attempt.Cancelled)
            return true;

        if (attempt.Blocker != null && popupUid != null)
            _popup.PopupClient(Loc.GetString("food-system-remove-mask", ("entity", attempt.Blocker.Value)), user, popupUid.Value);

        return false;
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
    /// <param name="feed">Whether to actually feed the item and eat the reagents</param>
    /// <returns></returns>
    private bool CanFeed(EntityUid user, EntityUid target, Entity<EdibleComponent?> ingested, bool feed = true)
    {
        if (!Resolve(ingested, ref ingested.Comp, false))
            return false;

        if (!HasMouthAvailable(target, user))
            return false;

        // If we don't have the tools to eat we can't eat.
        if (!TryGetSolution(ingested!, user, out var solution, out _))
            return false;

        return TryIngest(user, target, ingested, solution, feed);
    }

    /// <summary>
    /// Checks if we can get the solution from the entity we're trying to eat given our method of eating.
    /// Doesn't return the solution
    /// </summary>
    private bool TryGetSolution(Entity<EdibleComponent> ingested, EntityUid user)
    {
        return TryGetSolution(ingested, user, out _, out _);
    }

    /// <summary>
    /// Tries to get the solution we're trying to eat.
    /// </summary>
    private bool TryGetSolution(Entity<EdibleComponent, SolutionContainerManagerComponent?> ingested, EntityUid user,  [NotNullWhen(true)] out Entity<SolutionComponent>? solution, [NotNullWhen(true)] out Solution? sol)
    {
        solution = null;
        sol = null;

        if (!Resolve(ingested, ref ingested.Comp2))
            return false;

        var ev = new EdibleEvent
        {
            User = user,
            Destroy = ingested.Comp1.DeleteOnEmpty
        };
        RaiseLocalEvent(ingested, ref ev);

        if (ev.Cancelled)
            return false;

        if (ingested.Comp1.UtensilRequired && TryGetRequiredUtensils(user, ingested!, out _))
            return false;

        // Actually try and get the solution we're looking for.
        if (!_solutionContainer.TryGetSolution(ingested.Owner, ingested.Comp1.Solution, out solution, out sol))
            return false;

        //if (solution.Volume > 0)
            return true;

        // If we're here then our solution is empty
        if (ingested.Comp1.DeleteOnEmpty)
            Log.Debug("Remember to delete this and spawn trash.");
        else
            Log.Debug("Remember to popup about how the solution is empty.");

        return false;
    }

    public bool CanDigest(Entity<EdibleComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return false;

        return !_mobState.IsAlive(entity) || !entity.Comp.RequireDead;
    }

    /// <summary>
    ///     Returns true if <paramref name="stomachs"/> has a <see cref="StomachComponent.SpecialDigestible"/> that whitelists
    ///     this <paramref name="food"/> (or if they even have enough stomachs in the first place).
    /// </summary>
    private bool IsDigestibleBy(Entity<EdibleComponent?> food, List<Entity<StomachComponent, OrganComponent>> stomachs)
    {
        if (!Resolve(food, ref food.Comp, false))
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

            // If their diet is whitelist exclusive, then they cannot eat anything but what follows their whitelisted tags. Else, they can eat their tags AND human food.
            if (ent.Comp1.IsSpecialDigestibleExclusive)
                return false;
        }

        if (food.Comp.RequiresSpecialDigestion)
            return false;

        return true;
    }

    #region BodySystem Handlers

    private void OnTryIngest(Entity<BodyComponent> entity, ref CanIngestEvent args)
    {
        var food = args.Ingested;

        if (args.Handled || !Resolve(food, ref food.Comp))
            return;

        if (!CanDigest(args.Ingested))
            return;

        if (!_body.TryGetBodyOrganEntityComps<StomachComponent>(entity!, out var stomachs))
            return;

        // Can we digest the specific item we're trying to eat?
        if (!IsDigestibleBy(args.Ingested, stomachs))
            return;

        args.Handled = _doAfter.TryStartDoAfter(GetEatingDoAfterArgs(args.User, entity, food!));
        // TODO: Admin logs here ;_;
    }

    private void OnEatingDoAfter(Entity<BodyComponent> entity, ref EatingDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || entity.Comp.Deleted || args.Target == null)
            return;

        var food = args.Target.Value;

        if (!CanFeed(args.User, entity, food, false))
            return;

        if (!CanDigest(food))
            return;

        if (!_body.TryGetBodyOrganEntityComps<StomachComponent>(entity!, out var stomachs))
            return;

        // Can we digest the specific item we're trying to eat?
        if (!IsDigestibleBy(food, stomachs))
            return;

        // TODO: Everything below this needs to be rewritten (And maybe everything above moved to its own method).

        if (!TryComp<EdibleComponent>(food, out var edible) || !TryGetSolution((food, edible), args.User, out var solution, out var sol))
            return;

        var split = _solutionContainer.SplitSolution(solution.Value, edible.TransferAmount ?? FixedPoint2.Epsilon); // TODO: Fix this

        var forceFeed = args.User != entity.Owner;

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
            _solutionContainer.TryAddSolution(solution.Value, split);
            _popup.PopupClient(forceFeed ? Loc.GetString("food-system-you-cannot-eat-any-more-other", ("target", args.Target.Value)) : Loc.GetString("food-system-you-cannot-eat-any-more"), args.Target.Value, args.User);
            return;
        }

        // TODO: FUCK
        _reaction.DoEntityReaction(args.Target.Value, sol, ReactionMethod.Ingestion);
        _stomach.TryTransferSolution(stomachToUse.Value.Owner, split, stomachToUse);

        var flavors = _flavorProfile.GetLocalizedFlavorsMessage(food, args.User, sol);

        if (forceFeed)
        {
            var targetName = Identity.Entity(args.Target.Value, EntityManager);
            var userName = Identity.Entity(args.User, EntityManager);
            _popup.PopupEntity(Loc.GetString("food-system-force-feed-success", ("user", userName), ("flavors", flavors)), entity.Owner, entity.Owner);

            _popup.PopupClient(Loc.GetString("food-system-force-feed-success-user", ("target", targetName)), args.User, args.User);

            // log successful force feed
            _adminLogger.Add(LogType.ForceFeed, LogImpact.Medium, $"{ToPrettyString(entity.Owner):user} forced {ToPrettyString(args.User):target} to eat {ToPrettyString(entity.Owner):food}");
        }
        else
        {
            _popup.PopupClient(Loc.GetString(edible.EatMessage, ("food", entity.Owner), ("flavors", flavors)), args.User, args.User);

            // log successful voluntary eating
            _adminLogger.Add(LogType.Ingestion, LogImpact.Low, $"{ToPrettyString(args.User):target} ate {ToPrettyString(entity.Owner):food}");
        }

        _audio.PlayPredicted(edible.UseSound, args.Target.Value, args.User, AudioParams.Default.WithVolume(-1f).WithVariation(0.20f));

        // TODO: Utensils

        args.Repeat = !forceFeed;

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
        DeleteAndSpawnTrash((food, edible), args.User);

        var ev = new EatenEvent(entity);
        RaiseLocalEvent(args.Target.Value, ref ev);
    }

    // TODO: Just get shit working first then make it all nice later
    /// <summary>
    /// Gets the DoAfterArgs for the specific event
    /// </summary>
    /// <param name="user"></param>
    /// <param name="target"></param>
    /// <param name="consumer"></param>
    /// <param name="food"></param>
    /// <param name="solution"></param>
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
}
