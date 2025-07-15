using Content.Shared.Administration.Logs;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
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
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared.Nutrition.EntitySystems;

public abstract partial class SharedDrinkSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly FlavorProfileSystem _flavorProfile = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly IngestionSystem _ingestion = default!;
    [Dependency] private readonly OpenableSystem _openable = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DrinkComponent, ExaminedEvent>(OnExamined);

        // TODO: Kill above subscriptions

        SubscribeLocalEvent<DrinkComponent, UseInHandEvent>(OnUseDrinkInHand, after: new[] { typeof(OpenableSystem), typeof(InventorySystem) });
        SubscribeLocalEvent<DrinkComponent, AfterInteractEvent>(OnUseDrink);

        SubscribeLocalEvent<DrinkComponent, AttemptShakeEvent>(OnAttemptShake);

        SubscribeLocalEvent<DrinkComponent, GetVerbsEvent<AlternativeVerb>>(AddDrinkVerb);

        SubscribeLocalEvent<DrinkComponent, BeforeEatenEvent>(OnBeforeDrinkEaten);
        SubscribeLocalEvent<DrinkComponent, EatenEvent>(OnDrinkEaten);

        SubscribeLocalEvent<DrinkComponent, EdibleEvent>(OnDrink);

        SubscribeLocalEvent<DrinkComponent, IsDigestibleEvent>(OnIsDigestible);
    }

    protected void OnAttemptShake(Entity<DrinkComponent> entity, ref AttemptShakeEvent args)
    {
        if (IsEmpty(entity, entity.Comp))
            args.Cancelled = true;
    }

    protected void OnExamined(Entity<DrinkComponent> entity, ref ExaminedEvent args)
    {
        TryComp<OpenableComponent>(entity, out var openable);
        if (_openable.IsClosed(entity.Owner, null, openable, true) || !args.IsInDetailsRange || !entity.Comp.Examinable)
            return;

        var empty = IsEmpty(entity, entity.Comp);
        if (empty)
        {
            args.PushMarkup(Loc.GetString("drink-component-on-examine-is-empty"));
            return;
        }

        if (HasComp<ExaminableSolutionComponent>(entity))
        {
            //provide exact measurement for beakers
            args.PushText(Loc.GetString("drink-component-on-examine-exact-volume", ("amount", DrinkVolume(entity, entity.Comp))));
        }
        else
        {
            //general approximation
            var remainingString = (int) _solutionContainer.PercentFull(entity) switch
            {
                100 => "drink-component-on-examine-is-full",
                > 66 => "drink-component-on-examine-is-mostly-full",
                > 33 => HalfEmptyOrHalfFull(args),
                _ => "drink-component-on-examine-is-mostly-empty",
            };
            args.PushMarkup(Loc.GetString(remainingString));
        }
    }

    protected FixedPoint2 DrinkVolume(EntityUid uid, DrinkComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return FixedPoint2.Zero;

        if (!_solutionContainer.TryGetSolution(uid, component.Solution, out _, out var sol))
            return FixedPoint2.Zero;

        return sol.Volume;
    }

    protected bool IsEmpty(EntityUid uid, DrinkComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return true;

        return DrinkVolume(uid, component) <= 0;
    }

    // some see half empty, and others see half full
    private string HalfEmptyOrHalfFull(ExaminedEvent args)
    {
        string remainingString = "drink-component-on-examine-is-half-full";

        if (TryComp(args.Examiner, out MetaDataComponent? examiner) && examiner.EntityName.Length > 0
            && string.Compare(examiner.EntityName.Substring(0, 1), "m", StringComparison.InvariantCultureIgnoreCase) > 0)
            remainingString = "drink-component-on-examine-is-half-empty";

        return remainingString;
    }

    /// <summary>
    /// Tries to feed the drink item to the target entity
    /// </summary>
    protected bool TryDrink(EntityUid user, EntityUid target, DrinkComponent drink, EntityUid item)
    {
        if (!HasComp<BodyComponent>(target))
            return false;

        if (!_body.TryGetBodyOrganEntityComps<StomachComponent>(target, out var stomachs))
            return false;

        if (_openable.IsClosed(item, user, predicted: true))
            return true;

        if (!_solutionContainer.TryGetSolution(item, drink.Solution, out _, out var drinkSolution) || drinkSolution.Volume <= 0)
        {
            if (drink.IgnoreEmpty)
                return false;

            _popup.PopupClient(Loc.GetString("drink-component-try-use-drink-is-empty", ("entity", item)), item, user);
            return true;
        }

        //if (_food.IsMouthBlocked(target, user))
            //return true;

        if (!_interaction.InRangeUnobstructed(user, item, popup: true))
            return true;

        var forceDrink = user != target;

        if (forceDrink)
        {
            var userName = Identity.Entity(user, EntityManager);

            _popup.PopupEntity(Loc.GetString("drink-component-force-feed", ("user", userName)), user, target);

            // logging
            _adminLogger.Add(LogType.ForceFeed, LogImpact.High, $"{ToPrettyString(user):user} is forcing {ToPrettyString(target):target} to drink {ToPrettyString(item):drink} {SharedSolutionContainerSystem.ToPrettyString(drinkSolution)}");
        }
        else
        {
            // log voluntary drinking
            _adminLogger.Add(LogType.Ingestion, LogImpact.Low, $"{ToPrettyString(target):target} is drinking {ToPrettyString(item):drink} {SharedSolutionContainerSystem.ToPrettyString(drinkSolution)}");
        }

        var flavors = _flavorProfile.GetLocalizedFlavorsMessage(user, drinkSolution);

        var doAfterEventArgs = new DoAfterArgs(EntityManager,
            user,
            forceDrink ? drink.ForceFeedDelay : drink.Delay,
            new ConsumeDoAfterEvent(drink.Solution, flavors),
            eventTarget: item,
            target: target,
            used: item)
        {
            BreakOnHandChange = false,
            BreakOnMove = forceDrink,
            BreakOnDamage = true,
            MovementThreshold = 0.01f,
            DistanceThreshold = 1.0f,
            // do-after will stop if item is dropped when trying to feed someone else
            // or if the item started out in the user's own hands
            NeedHand = forceDrink || _hands.IsHolding(user, item),
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
        return true;
    }

    // TODO: All code above must die...

    /// <summary>
    /// Eat or drink an item
    /// </summary>
    private void OnUseDrinkInHand(Entity<DrinkComponent> entity, ref UseInHandEvent ev)
    {
        if (ev.Handled)
            return;

        ev.Handled = _ingestion.TryIngest(ev.User, ev.User, entity);
    }

    /// <summary>
    /// Feed someone else
    /// </summary>
    private void OnUseDrink(Entity<DrinkComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target == null || !args.CanReach)
            return;

        args.Handled = _ingestion.TryIngest(args.User, args.Target.Value, entity);
    }

    private void AddDrinkVerb(Entity<DrinkComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var user = args.User;

        if (entity.Owner == user || !args.CanInteract || !args.CanAccess)
            return;

        // TODO: This might not need to be a bool or even return anything???
        if (!_ingestion.TryGetIngestionVerb(user, entity, EdibleType.Drink, out var verb))
            return;

        args.Verbs.Add(verb);
    }

    private void OnBeforeDrinkEaten(Entity<DrinkComponent> food, ref BeforeEatenEvent args)
    {
        if (args.Cancelled)
            return;

        // Set it to transfer amount if it exists, otherwise eat the whole volume if possible.
        args.Transfer = food.Comp.TransferAmount;
    }

    private void OnDrinkEaten(Entity<DrinkComponent> entity, ref EatenEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        // TODO: This broke again I think...
        // TODO: USE THIS AS A REFERENCE FOR THE NEW GET VERBS SYSTEM

        _audio.PlayPredicted(entity.Comp.UseSound, args.Target, args.User, AudioParams.Default.WithVolume(-2f).WithVariation(0.25f));

        var flavors = _flavorProfile.GetLocalizedFlavorsMessage(entity.Owner, args.Target, args.Split);

        if (args.ForceFed)
        {
            var targetName = Identity.Entity(args.Target, EntityManager);
            var userName = Identity.Entity(args.User, EntityManager);

            _popup.PopupEntity(Loc.GetString("drink-component-force-feed-success", ("user", userName), ("flavors", flavors)), args.Target, args.Target);

            _popup.PopupEntity(
                Loc.GetString("drink-component-force-feed-success-user", ("target", targetName)),
                args.User, args.User);

            // log successful forced drinking
            _adminLogger.Add(LogType.ForceFeed, LogImpact.Medium, $"{ToPrettyString(entity.Owner):user} forced {ToPrettyString(args.User):target} to drink {ToPrettyString(entity.Owner):drink}");
        }
        else
        {
            _popup.PopupClient(Loc.GetString("drink-component-try-use-drink-success-slurp-taste", ("flavors", flavors)), args.User, args.User);
            _popup.PopupEntity(Loc.GetString("drink-component-try-use-drink-success-slurp"), args.User, Filter.PvsExcept(args.User), true);

            // log successful voluntary drinking
            _adminLogger.Add(LogType.Ingestion, LogImpact.Low, $"{ToPrettyString(args.User):target} drank {ToPrettyString(entity.Owner):drink}");
        }

        // BREAK OUR UTENSILS
        if (_ingestion.TryGetUtensils(args.User, entity, out var utensils))
        {
            foreach (var utensil in utensils)
            {
                _ingestion.TryBreak(utensil, args.User);
            }
        }

        if (_ingestion.GetUsesRemaining(entity, entity.Comp.Solution, args.Split.Volume) > 0)
            return;

        // Food is always destroyed...
        args.Destroy = true;
    }

    private void OnDrink(Entity<DrinkComponent> food, ref EdibleEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Cancelled || args.Solution != null)
            return;

        _solutionContainer.TryGetSolution(food.Owner, food.Comp.Solution, out args.Solution);
    }

    private void OnIsDigestible(Entity<DrinkComponent> ent, ref IsDigestibleEvent args)
    {
        // Anyone can drink from puddles on the floor!
        args.UniversalDigestion();
    }
}
