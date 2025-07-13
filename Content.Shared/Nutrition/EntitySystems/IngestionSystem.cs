using Content.Shared.Administration.Logs;
using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry;
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

        // Interactions
        SubscribeLocalEvent<EdibleComponent, UseInHandEvent>(OnUseFoodInHand, after: new[] { typeof(OpenableSystem), typeof(InventorySystem) });
        SubscribeLocalEvent<EdibleComponent, AfterInteractEvent>(OnFeedFood);

        // Generic Eating Handlers
        SubscribeLocalEvent<EdibleComponent, BeforeEatenEvent>(OnBeforeEaten);
        SubscribeLocalEvent<EdibleComponent, EatenEvent>(OnEdibleEaten);
        SubscribeLocalEvent<EdibleComponent, FullyEatenEvent>(OnFullyEaten);

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

    #region BodySystem

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
    /// Overflow method which takes a single stomach into account.
    /// </summary>
    private bool IsDigestibleBy(Entity<EdibleComponent?> food, Entity<StomachComponent, OrganComponent> stomach)
    {
        if (!Resolve(food, ref food.Comp, false))
            return false;

        if (food.Comp.RequiresSpecialDigestion)
            return _whitelistSystem.IsWhitelistPass(stomach.Comp1.SpecialDigestible, food);

        if (stomach.Comp1.SpecialDigestible == null || !stomach.Comp1.IsSpecialDigestibleExclusive || _whitelistSystem.IsWhitelistPass(stomach.Comp1.SpecialDigestible, food))
            return true;

        return false;
    }

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
        if (!CanIngestEdible(args.User, entity, args.Ingested))
            return;

        // Utensils?

        args.Handled = _doAfter.TryStartDoAfter(GetEatingDoAfterArgs(args.User, entity, food!));
        // TODO: Admin logs here ;_;
    }

    private void OnEatingDoAfter(Entity<BodyComponent> entity, ref EatingDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || entity.Comp.Deleted || args.Target == null)
            return;

        var food = args.Target.Value;

        if (!CanIngestEdible(args.User, entity, food, out var solution))
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
        if (TryGetUtensils(args.User, entity.Comp, out var utensils))
        {
            foreach (var utensil in utensils)
            {
                TryBreak(utensil, args.User);
            }
        }

        if (GetUsesRemaining(entity!) > 0)
            return;

        args.Destroy = entity.Comp.DeleteOnEmpty;
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
}
