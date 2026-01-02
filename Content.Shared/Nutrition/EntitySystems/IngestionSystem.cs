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
using Content.Shared.Forensics;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Tools.EntitySystems;
using Content.Shared.UserInterface;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.Nutrition.EntitySystems;

/// <remarks>
/// I was warned about puddle system, I knew the risks with body system, but food and drink system?
/// Food and Drink system was a sleeping titan, and I walked directly into it's gaping maw.
/// Between copy-pasted code, strange reliance on systems, being a pillar of chemistry for some reason,
/// nothing could've prepared me for the horror that I had to endure. I saw the signs, comments of those who
/// turned back, code that was made to be "just good enough" the fact that I got soaped by soap.yml, but I
/// ignored them and pressed on.
/// Let this remark be a reminder to those who come after, that I was here, and that I vanquished a great beast.
/// Let young little contributors rest easy at night not knowing the horrible system that once lived beneath the
/// bedrock of the codebase they now commit to.
/// </remarks>
/// <summary>
/// This handles the ingestion of solutions and entities.
/// </summary>
public sealed partial class IngestionSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly FlavorProfileSystem _flavorProfile = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
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
        SubscribeLocalEvent<EdibleComponent, UseInHandEvent>(OnUseEdibleInHand, after: [typeof(OpenableSystem), typeof(InventorySystem), typeof(ActivatableUISystem)]);
        SubscribeLocalEvent<EdibleComponent, AfterInteractEvent>(OnEdibleInteract, after: [typeof(ToolOpenableSystem)]);

        // Generic Eating Handlers
        SubscribeLocalEvent<EdibleComponent, BeforeIngestedEvent>(OnBeforeIngested);
        SubscribeLocalEvent<EdibleComponent, IngestedEvent>(OnEdibleIngested);
        SubscribeLocalEvent<EdibleComponent, FullyEatenEvent>(OnFullyEaten);

        // Body Component eating handler
        SubscribeLocalEvent<BodyComponent, AttemptIngestEvent>(OnTryIngest);
        SubscribeLocalEvent<BodyComponent, EatingDoAfterEvent>(OnEatingDoAfter);

        // Verbs
        SubscribeLocalEvent<EdibleComponent, GetVerbsEvent<AlternativeVerb>>(AddEdibleVerbs);
        SubscribeLocalEvent<EdibleComponent, SolutionContainerChangedEvent>(OnSolutionContainerChanged);

        // Misc
        SubscribeLocalEvent<EdibleComponent, AttemptShakeEvent>(OnAttemptShake);
        SubscribeLocalEvent<EdibleComponent, BeforeFullySlicedEvent>(OnBeforeFullySliced);

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

        ev.Handled = TryIngest(ev.User, entity);
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

    /// <summary>Raises events to see if it's possible to ingest </summary>
    /// <param name="user">The entity who is trying to make this happen.</param>
    /// <param name="target">The entity who is being made to ingest something.</param>
    /// <param name="ingested">The entity that is trying to be ingested.</param>
    /// <param name="ingest"> When set to true, it tries to ingest. When false, it only checks if we can.</param>
    /// <returns>Returns true if we can ingest the item.</returns>
    private bool AttemptIngest(EntityUid user, EntityUid target, EntityUid ingested, bool ingest)
    {
        var eatEv = new IngestibleEvent();
        RaiseLocalEvent(ingested, ref eatEv);

        if (eatEv.Cancelled)
            return false;

        var ingestionEv = new AttemptIngestEvent(user, ingested, ingest);
        RaiseLocalEvent(target, ref ingestionEv);

        return ingestionEv.Handled;
    }

    private void OnEdibleInit(Entity<EdibleComponent> entity, ref ComponentInit args)
    {
        // Beakers, Soap and other items have drainable, and we should be able to eat that solution.
        // This ensures that tests fail when you configured the yaml from and EdibleComponent uses the wrong solution,
        if (TryComp<DrainableSolutionComponent>(entity, out var existingDrainable))
            entity.Comp.Solution = existingDrainable.Solution;
        else
            _solutionContainer.EnsureSolution(entity.Owner, entity.Comp.Solution, out _);

        UpdateAppearance(entity);

        if (TryComp(entity, out RefillableSolutionComponent? refillComp))
            refillComp.Solution = entity.Comp.Solution;
    }

    #region Appearance System

    public void UpdateAppearance(Entity<EdibleComponent, AppearanceComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp2, false))
            return;

        var drainAvailable = EdibleVolume(entity);
        _appearance.SetData(entity, FoodVisuals.Visual, drainAvailable.Float(), entity.Comp2);
    }

    private void OnSolutionContainerChanged(Entity<EdibleComponent> entity, ref SolutionContainerChangedEvent args)
    {
        UpdateAppearance(entity);
    }

    #endregion

    #region BodySystem

    // TODO: The IsDigestibleBy bools should be API but they're too specific to the BodySystem to be API. Requires BodySystem rework.
    /// <summary>
    /// Generic method which takes a list of stomachs, and checks if a given food item passes any stomach's whitelist
    /// in a given list of stomachs.
    /// </summary>
    /// <param name="food">Entity being eaten</param>
    /// <param name="stomachs">Stomachs available to digest</param>
    /// <param name="popup">Should we also display popup text if it exists?</param>
    public bool IsDigestibleBy(EntityUid food, List<Entity<StomachComponent, OrganComponent>> stomachs, out bool popup)
    {
        popup = false;
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
        popup = true;
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

        if (ev.Universal)
            return true;

        if (ev.SpecialDigestion)
            return _whitelistSystem.IsWhitelistPass(stomach.Comp1.SpecialDigestible, food);

        if (stomach.Comp1.SpecialDigestible == null || !stomach.Comp1.IsSpecialDigestibleExclusive || _whitelistSystem.IsWhitelistPass(stomach.Comp1.SpecialDigestible, food))
            return true;

        return false;
    }

    private void OnTryIngest(Entity<BodyComponent> entity, ref AttemptIngestEvent args)
    {
        var food = args.Ingested;
        var forceFed = args.User != entity.Owner;

        if (!_body.TryGetBodyOrganEntityComps<StomachComponent>(entity!, out var stomachs))
            return;

        // Can we digest the specific item we're trying to eat?
        if (!IsDigestibleBy(args.Ingested, stomachs, out var popup))
        {
            if (!args.Ingest || !popup)
                return;

            if (forceFed)
                _popup.PopupClient(Loc.GetString("ingestion-cant-digest-other", ("target", entity), ("entity", food)), entity, args.User);
            else
                _popup.PopupClient(Loc.GetString("ingestion-cant-digest", ("entity", food)), entity, entity);

            return;
        }

        // Exit early if we're just trying to get verbs
        if (!args.Ingest)
        {
            args.Handled = true;
            return;
        }

        // Check if despite being able to digest the item something is blocking us from eating.
        if (!CanConsume(args.User, entity, args.Ingested, out var solution, out var time))
            return;

        if (!_doAfter.TryStartDoAfter(GetEdibleDoAfterArgs(args.User, entity, food, time ?? TimeSpan.Zero)))
            return;

        args.Handled = true;
        var foodSolution = solution.Value.Comp.Solution;

        if (forceFed)
        {
            var userName = Identity.Entity(args.User, EntityManager);
            _popup.PopupEntity(Loc.GetString("edible-force-feed", ("user", userName), ("verb", GetEdibleVerb(food))), args.User, entity);

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

        var blockerEv = new IngestibleEvent();
        RaiseLocalEvent(food, ref blockerEv);

        if (blockerEv.Cancelled)
            return;

        if (!CanConsume(args.User, entity, food, out var solution, out _))
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
            // Very long
            _popup.PopupClient(Loc.GetString("ingestion-you-cannot-ingest-any-more", ("verb", GetEdibleVerb(food))), entity, entity);
            if (!forceFed)
                return;

            _popup.PopupClient(Loc.GetString("ingestion-other-cannot-ingest-any-more", ("target", entity), ("verb", GetEdibleVerb(food))), args.Target.Value, args.User);
            return;
        }

        var beforeEv = new BeforeIngestedEvent(FixedPoint2.Zero, highestAvailable, solution.Value.Comp.Solution);
        RaiseLocalEvent(food, ref beforeEv);
        RaiseLocalEvent(entity, ref beforeEv);

        if (beforeEv.Cancelled || beforeEv.Min > beforeEv.Max)
        {
            // Very long x2
            _popup.PopupClient(Loc.GetString("ingestion-you-cannot-ingest-any-more", ("verb", GetEdibleVerb(food))), entity, entity);
            if (!forceFed)
                return;

            _popup.PopupClient(Loc.GetString("ingestion-other-cannot-ingest-any-more", ("target", entity), ("verb", GetEdibleVerb(food))), args.Target.Value, args.User);
            return;
        }

        var transfer = FixedPoint2.Clamp(beforeEv.Transfer, beforeEv.Min, beforeEv.Max);

        var split = _solutionContainer.SplitSolution(solution.Value, transfer);

        if (beforeEv.Refresh)
            _solutionContainer.TryAddSolution(solution.Value, split);

        var ingestEv = new IngestingEvent(food, split, forceFed);
        RaiseLocalEvent(entity, ref ingestEv);

        _reaction.DoEntityReaction(entity, split, ReactionMethod.Ingestion);

        // Everything is good to go item has been successfuly eaten
        var afterEv = new IngestedEvent(args.User, entity, split, forceFed);
        RaiseLocalEvent(food, ref afterEv);

        _stomach.TryTransferSolution(stomachToUse.Value.Owner, split, stomachToUse);

        if (!afterEv.Destroy)
        {
            args.Repeat = afterEv.Repeat;
            return;
        }

        var ev = new DestructionAttemptEvent();
        RaiseLocalEvent(food, ev);
        if (ev.Cancelled)
            return;

        // Tell the food that it's time to die.
        var finishedEv = new FullyEatenEvent(args.User);
        RaiseLocalEvent(food, ref finishedEv);

        var eventArgs = new DestructionEventArgs();
        RaiseLocalEvent(food, eventArgs);

        PredictedDel(food);

        // Don't try to repeat if its being deleted
        args.Repeat = false;
    }

    /// <summary>
    /// Gets the DoAfterArgs for the specific event
    /// </summary>
    /// <param name="user">Entity that is doing the action.</param>
    /// <param name="target">Entity that is eating.</param>
    /// <param name="food">Food entity we're trying to eat.</param>
    /// <param name="delay">The time delay for our DoAfter</param>
    /// <returns>Returns true if it was able to successfully start the DoAfter</returns>
    private DoAfterArgs GetEdibleDoAfterArgs(EntityUid user, EntityUid target, EntityUid food, TimeSpan delay = default)
    {
        var forceFeed = user != target;

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

    private void OnBeforeIngested(Entity<EdibleComponent> food, ref BeforeIngestedEvent args)
    {
        if (args.Cancelled || args.Solution is not { } solution)
            return;

        // Set it to transfer amount if it exists, otherwise eat the whole volume if possible.
        args.Transfer = food.Comp.TransferAmount ?? solution.Volume;
    }

    private void OnEdibleIngested(Entity<EdibleComponent> entity, ref IngestedEvent args)
    {
        // This is a lot but there wasn't really a way to separate this from the EdibleComponent otherwise I would've moved it.

        if (args.Handled)
            return;

        args.Handled = true;

        var edible = _proto.Index(entity.Comp.Edible);

        _audio.PlayPredicted(entity.Comp.UseSound ?? edible.UseSound, args.Target, args.User);

        var flavors = _flavorProfile.GetLocalizedFlavorsMessage(entity.Owner, args.Target, args.Split);

        if (args.ForceFed)
        {
            var targetName = Identity.Entity(args.Target, EntityManager);
            var userName = Identity.Entity(args.User, EntityManager);
            _popup.PopupEntity(Loc.GetString("edible-force-feed-success", ("user", userName), ("verb", edible.Verb), ("flavors", flavors)), entity, entity);

            _popup.PopupClient(Loc.GetString("edible-force-feed-success-user", ("target", targetName), ("verb", edible.Verb)), args.User, args.User);

            // log successful forced feeding
            // TODO: Use correct verb
            _adminLogger.Add(LogType.ForceFeed, LogImpact.Medium, $"{ToPrettyString(entity):user} forced {ToPrettyString(args.User):target} to eat {ToPrettyString(entity):food}");
        }
        else
        {
            _popup.PopupPredicted(Loc.GetString(edible.Message, ("food", entity.Owner), ("flavors", flavors)),
                Loc.GetString(edible.OtherMessage),
                args.User,
                args.User);

            // log successful voluntary eating
            // TODO: Use correct verb
            // the past tense is tricky here
            // localized admin logs when?
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

        // This also prevents us from repeating if it's empty
        if (!IsEmpty(entity))
        {
            // Leave some of the consumer's DNA on the consumed item...
            var ev = new TransferDnaEvent
            {
                Donor = args.Target,
                Recipient = entity,
                CanDnaBeCleaned = false,
            };
            RaiseLocalEvent(args.Target, ref ev);

            args.Repeat = !args.ForceFed;
            return;
        }

        args.Destroy = entity.Comp.DestroyOnEmpty;
    }

    private void OnFullyEaten(Entity<EdibleComponent> entity, ref FullyEatenEvent args)
    {
        SpawnTrash(entity, args.User);
    }

    private void OnBeforeFullySliced(Entity<EdibleComponent> entity, ref BeforeFullySlicedEvent args)
    {
        SpawnTrash(entity, args.User);
    }

    private void AddEdibleVerbs(Entity<EdibleComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var user = args.User;

        if (entity.Owner == user || !args.CanInteract || !args.CanAccess)
            return;

        if (!TryGetIngestionVerb(user, entity, entity.Comp.Edible, out var verb))
            return;

        args.Verbs.Add(verb);
    }

    private void OnAttemptShake(Entity<EdibleComponent> entity, ref AttemptShakeEvent args)
    {
        if (IsEmpty(entity))
            args.Cancelled = true;
    }
}
