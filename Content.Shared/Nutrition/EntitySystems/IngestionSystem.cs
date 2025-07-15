using Content.Shared.Administration.Logs;
using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
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
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;

namespace Content.Shared.Nutrition.EntitySystems;

/// <summary>
/// This handles the ingestion of solutions.
/// </summary>
public sealed partial class IngestionSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly FlavorProfileSystem _flavorProfile = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedDestructibleSystem _destruct = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    // Body Component Dependencies
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly ReactiveSystem _reaction = default!;
    [Dependency] private readonly StomachSystem _stomach = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EdibleComponent, ComponentInit>(OnEdibleInit);

        // Interactions
        SubscribeLocalEvent<EdibleComponent, UseInHandEvent>(OnUseEdibleInHand, after: new[] { typeof(OpenableSystem), typeof(InventorySystem) });
        SubscribeLocalEvent<EdibleComponent, AfterInteractEvent>(OnEdibleInteract);

        // Generic Eating Handlers
        SubscribeLocalEvent<EdibleComponent, BeforeEatenEvent>(OnBeforeEaten);
        SubscribeLocalEvent<EdibleComponent, EatenEvent>(OnEdibleEaten);
        SubscribeLocalEvent<EdibleComponent, FullyEatenEvent>(OnFullyEaten);

        // Body Component eating handler
        SubscribeLocalEvent<BodyComponent, CanIngestEvent>(OnTryIngest);
        SubscribeLocalEvent<BodyComponent, EatingDoAfterEvent>(OnEatingDoAfter);

        // Verbs
        SubscribeLocalEvent<EdibleComponent, GetVerbsEvent<AlternativeVerb>>(AddEdibleVerbs);

        // Misc
        SubscribeLocalEvent<EdibleComponent, AttemptShakeEvent>(OnAttemptShake);

        InitializeBlockers();
        InitializeUtensils();
    }

    /// <summary>
    /// Eat or drink an item
    /// </summary>
    private void OnUseEdibleInHand(Entity<EdibleComponent> entity, ref UseInHandEvent ev)
    {
        if (ev.Handled)
            return;

        ev.Handled = TryIngest(ev.User, ev.User, entity);
    }

    /// <summary>
    /// Feed someone else
    /// </summary>
    private void OnEdibleInteract(Entity<EdibleComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target == null || !args.CanReach)
            return;

        args.Handled = TryIngest(args.User, args.Target.Value, entity);
    }

    private void OnEdibleInit(Entity<EdibleComponent> entity, ref ComponentInit args)
    {
        // Beakers, Soap and other items have drainable, and we should be able to eat that solution...
        // If I could make drainable properly support sound effects and such I'd just have it use TryIngest itself
        if (TryComp<DrainableSolutionComponent>(entity, out var existingDrainable))
            entity.Comp.Solution = existingDrainable.Solution;
        else
            _solutionContainer.EnsureSolution(entity.Owner, entity.Comp.Solution, out _);

        UpdateAppearance(entity);

        // TODO: Why the fuck does this exist???
        if (TryComp(entity, out RefillableSolutionComponent? refillComp))
            refillComp.Solution = entity.Comp.Solution;
    }

    // TODO: Fix this method.
    public void UpdateAppearance(Entity<EdibleComponent, AppearanceComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp2, false))
            return;

        var drainAvailable = EdibleVolume(entity);
        _appearance.SetData(entity, FoodVisuals.Visual, drainAvailable.Float(), entity.Comp2);
    }


    #region BodySystem

    // TODO: The IsDigestibleBy bools should be API but they're too specific to the BodySystem to be API. Requires BodySystem rework.
    /// <summary>
    /// Generic method which takes a list of stomachs, and checks if a given food item passes any stomach's whitelist
    /// in a given list of stomachs.
    /// </summary>
    /// <param name="food">Entity being eaten</param>
    /// <param name="stomachs">Stomachs available to digest</param>
    public bool IsDigestibleBy(EntityUid food, List<Entity<StomachComponent, OrganComponent>> stomachs)
    {
        var ev = new IsDigestibleEvent();
        RaiseLocalEvent(food, ref ev);

        if (!ev.Digestible)
            return false;

        if (ev.Universal)
            return true;

        if (ev.SpecialDigestion)
        {
            foreach (var ent in stomachs)
            {
                // We need one stomach that can digest our special food.
                if (_whitelistSystem.IsWhitelistPass(ent.Comp1.SpecialDigestible, food))
                    return true;
            }
        }
        else
        {
            foreach (var ent in stomachs)
            {
                // We need one stomach that can digest normal food.
                if (ent.Comp1.SpecialDigestible == null
                    || !ent.Comp1.IsSpecialDigestibleExclusive
                    || _whitelistSystem.IsWhitelistPass(ent.Comp1.SpecialDigestible, food))
                    return true;
            }
        }

        // If we didn't find a stomach that can digest our food then it doesn't exist.
        return false;
    }

    /// <summary>
    /// Generic method which takes a single stomach into account, and checks if a given food item passes a stomach whitelist.
    /// </summary>
    /// <param name="food">Entity being eaten</param>
    /// <param name="stomach">Stomachs that is attempting to digest.</param>
    public bool IsDigestibleBy(EntityUid food, Entity<StomachComponent, OrganComponent> stomach)
    {
        var ev = new IsDigestibleEvent();
        RaiseLocalEvent(food, ref ev);

        if (!ev.Digestible)
            return false;

        if (ev.SpecialDigestion)
            return _whitelistSystem.IsWhitelistPass(stomach.Comp1.SpecialDigestible, food);

        if (stomach.Comp1.SpecialDigestible == null || !stomach.Comp1.IsSpecialDigestibleExclusive || _whitelistSystem.IsWhitelistPass(stomach.Comp1.SpecialDigestible, food))
            return true;

        return false;
    }

    private void OnTryIngest(Entity<BodyComponent> entity, ref CanIngestEvent args)
    {
        var food = args.Ingested;

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
        if (!CanIngest(args.User, entity, args.Ingested, out var solution))
            return;

        if (!_doAfter.TryStartDoAfter(GetEdibleDoAfterArgs(args.User, entity, food)))
            return;

        args.Handled = true;
        var foodSolution = solution.Value.Comp.Solution;

        if (args.User != entity.Owner)
        {
            var userName = Identity.Entity(args.User, EntityManager);
            _popup.PopupEntity(Loc.GetString("food-system-force-feed", ("user", userName)), args.User, entity);

            // logging
            _adminLogger.Add(LogType.ForceFeed, LogImpact.Medium, $"{ToPrettyString(args.User):user} is forcing {ToPrettyString(entity):target} to eat {ToPrettyString(food):food} {SharedSolutionContainerSystem.ToPrettyString(foodSolution)}");
        }
        else
        {
            // log voluntary eating
            _adminLogger.Add(LogType.Ingestion, LogImpact.Low, $"{ToPrettyString(entity):target} is eating {ToPrettyString(food):food} {SharedSolutionContainerSystem.ToPrettyString(foodSolution)}");
        }
    }

    private void OnEatingDoAfter(Entity<BodyComponent> entity, ref EatingDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || entity.Comp.Deleted || args.Target == null)
            return;

        var food = args.Target.Value;

        if (!CanIngest(args.User, entity, food, out var solution))
            return;

        if (!_body.TryGetBodyOrganEntityComps<StomachComponent>(entity!, out var stomachs))
            return;

        var forceFed = args.User != entity.Owner;

        var highestAvailable = FixedPoint2.Zero;
        Entity<StomachComponent>? stomachToUse = null;
        foreach (var ent in stomachs)
        {
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

        // All stomachs are full or we have no stomachs
        if (stomachToUse == null)
        {
            _popup.PopupClient(forceFed ? Loc.GetString("food-system-you-cannot-eat-any-more-other", ("target", args.Target.Value)) : Loc.GetString("food-system-you-cannot-eat-any-more"), args.Target.Value, args.User);
            return;
        }

        var beforeEv = new BeforeEatenEvent(args.User, args.Target.Value, FixedPoint2.Zero, highestAvailable, solution.Value.Comp.Solution);
        RaiseLocalEvent(food, ref beforeEv);

        if (beforeEv.Cancelled)
            return;

        var transfer = FixedPoint2.Clamp(beforeEv.Transfer, beforeEv.Min, beforeEv.Max);

        var split = _solutionContainer.SplitSolution(solution.Value, transfer);

        // Everything is good to go item has been successfuly eaten
        var afterEv = new EatenEvent(args.User, args.Target.Value, split, forceFed);
        RaiseLocalEvent(food, ref afterEv);

        if (afterEv.Refresh)
            _solutionContainer.TryAddSolution(solution.Value, split);

        _reaction.DoEntityReaction(args.Target.Value, split, ReactionMethod.Ingestion);
        _stomach.TryTransferSolution(stomachToUse.Value.Owner, split, stomachToUse);

        if (!afterEv.Destroy)
        {
            args.Repeat = !forceFed;
            return;
        }

        // If we can't destroy it, it's too powerful to spawn trash and do other things...
        if (!_destruct.DestroyEntity(food))
            return;

        // Tell the food that it's time to die.
        var finishedEv = new FullyEatenEvent(args.User);
        RaiseLocalEvent(food, ref finishedEv);

        // Don't try to repeat if its being deleted
        args.Repeat = false;
    }

    /// <summary>
    /// Gets the DoAfterArgs for the specific event
    /// </summary>
    /// <param name="user">Entity that is doing the action.</param>
    /// <param name="target">Entity that is eating.</param>
    /// <param name="food">Food entity we're trying to eat.</param>
    /// <param name="delay">The time delay for our DoAfter</param> // TODO: Make sure to actually initialize this later...
    /// <returns>Returns true if it was able to successfully start the DoAfter</returns>
    private DoAfterArgs GetEdibleDoAfterArgs(EntityUid user, EntityUid target, EntityUid food, TimeSpan delay = default)
    {
        var forceFeed = user != target;

        // TODO: Either use one of the existing events, or make a new event to get the DoAfterTime for eating...

        var doAfterArgs = new DoAfterArgs(EntityManager, user, delay, new EatingDoAfterEvent(), target, food)
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

    private void OnBeforeEaten(Entity<EdibleComponent> food, ref BeforeEatenEvent args)
    {
        if (args.Cancelled || args.Solution is not { } solution)
            return;

        // Set it to transfer amount if it exists, otherwise eat the whole volume if possible.
        args.Transfer = food.Comp.TransferAmount ?? solution.Volume;
    }

    private void OnEdibleEaten(Entity<EdibleComponent> entity, ref EatenEvent args)
    {
        // This is a lot but there wasn't really a way to separate this from the EdibleComponent otherwise I would've moved it.

        if (args.Handled)
            return;

        args.Handled = true;

        _audio.PlayPredicted(entity.Comp.UseSound, args.Target, args.User, AudioParams.Default.WithVolume(-1f).WithVariation(0.20f));

        var flavors = _flavorProfile.GetLocalizedFlavorsMessage(entity.Owner, args.Target, args.Split);

        if (args.ForceFed)
        {
            var targetName = Identity.Entity(args.Target, EntityManager);
            var userName = Identity.Entity(args.User, EntityManager);
            _popup.PopupEntity(Loc.GetString("food-system-force-feed-success", ("user", userName), ("flavors", flavors)), entity, entity);

            _popup.PopupClient(Loc.GetString("food-system-force-feed-success-user", ("target", targetName)), args.User, args.User);

            // log successful forced feeding
            _adminLogger.Add(LogType.ForceFeed, LogImpact.Medium, $"{ToPrettyString(entity):user} forced {ToPrettyString(args.User):target} to eat {ToPrettyString(entity):food}");
        }
        else
        {
            _popup.PopupClient(Loc.GetString(entity.Comp.EatMessage, ("food", entity.Owner), ("flavors", flavors)), args.User, args.User);

            // log successful voluntary eating
            _adminLogger.Add(LogType.Ingestion, LogImpact.Low, $"{ToPrettyString(args.User):target} ate {ToPrettyString(entity):food}");
        }

        // BREAK OUR UTENSILS
        if (TryGetUtensils(args.User, entity, out var utensils))
        {
            foreach (var utensil in utensils)
            {
                TryBreak(utensil, args.User);
            }
        }

        if (GetUsesRemaining(entity!, args.Split.Volume) > 0)
            return;

        args.Destroy = entity.Comp.DestroyOnEmpty;
    }

    private void OnFullyEaten(Entity<EdibleComponent> food, ref FullyEatenEvent args)
    {
        if (food.Comp.Trash.Count == 0)
            return;

        var position = _transform.GetMapCoordinates(food);
        var trashes = food.Comp.Trash;
        var tryPickup = _hands.IsHolding(args.User, food, out _);

        foreach (var trash in trashes)
        {
            var spawnedTrash = EntityManager.PredictedSpawn(trash, position);

            // If the user is holding the item
            if (tryPickup)
            {
                // Put the trash in the user's hand
                _hands.TryPickupAnyHand(args.User, spawnedTrash);
            }
        }
    }

    private void AddEdibleVerbs(Entity<EdibleComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var user = args.User;

        if (entity.Owner == user || !args.CanInteract || !args.CanAccess)
            return;

        // TODO: This might not need to be a bool or even return anything???
        if (!TryGetIngestionVerb(user, entity, entity.Comp.EdibleType, out var verb))
            return;

        args.Verbs.Add(verb);
    }

    private void OnAttemptShake(Entity<EdibleComponent> entity, ref AttemptShakeEvent args)
    {
        if (IsEmpty(entity))
            args.Cancelled = true;
    }
}
