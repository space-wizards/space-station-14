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
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared.Nutrition.EntitySystems;

public abstract partial class SharedDrinkSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly FlavorProfileSystem _flavorProfile = default!;
    [Dependency] private readonly FoodSystem _food = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly OpenableSystem _openable = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DrinkComponent, AttemptShakeEvent>(OnAttemptShake);
        SubscribeLocalEvent<DrinkComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<DrinkComponent, GetVerbsEvent<AlternativeVerb>>(AddDrinkVerb);
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

    private void AddDrinkVerb(Entity<DrinkComponent> entity, ref GetVerbsEvent<AlternativeVerb> ev)
    {
        if (entity.Owner == ev.User ||
            !ev.CanInteract ||
            !ev.CanAccess ||
            !TryComp<BodyComponent>(ev.User, out var body) ||
            !_body.TryGetBodyOrganEntityComps<StomachComponent>((ev.User, body), out var stomachs))
            return;

        // Make sure the solution exists
        if (!_solutionContainer.TryGetSolution(entity.Owner, entity.Comp.Solution, out var solution))
            return;

        // no drinking from living drinks, have to kill them first.
        if (_mobState.IsAlive(entity))
            return;

        var user = ev.User;
        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                TryDrink(user, user, entity.Comp, entity);
            },
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/drink.svg.192dpi.png")),
            Text = Loc.GetString("drink-system-verb-drink"),
            Priority = 2
        };

        ev.Verbs.Add(verb);
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

        if (_food.IsMouthBlocked(target, user))
            return true;

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
}
