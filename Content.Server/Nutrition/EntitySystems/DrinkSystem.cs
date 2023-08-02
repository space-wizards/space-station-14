using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Chemistry.ReagentEffects;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Forensics;
using Content.Server.Nutrition.Components;
using Content.Server.Popups;
using Content.Shared.Administration.Logs;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.Components;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Nutrition.EntitySystems;

public sealed class DrinkSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly FoodSystem _food = default!;
    [Dependency] private readonly FlavorProfileSystem _flavorProfile = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly PuddleSystem _puddleSystem = default!;
    [Dependency] private readonly ReactiveSystem _reaction = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly StomachSystem _stomach = default!;

    public override void Initialize()
    {
        base.Initialize();

        // TODO add InteractNoHandEvent for entities like mice.
        SubscribeLocalEvent<DrinkComponent, SolutionChangedEvent>(OnSolutionChange);
        SubscribeLocalEvent<DrinkComponent, ComponentInit>(OnDrinkInit);
        SubscribeLocalEvent<DrinkComponent, LandEvent>(HandleLand);
        SubscribeLocalEvent<DrinkComponent, UseInHandEvent>(OnUse);
        SubscribeLocalEvent<DrinkComponent, AfterInteractEvent>(AfterInteract);
        SubscribeLocalEvent<DrinkComponent, GetVerbsEvent<AlternativeVerb>>(AddDrinkVerb);
        SubscribeLocalEvent<DrinkComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<DrinkComponent, SolutionTransferAttemptEvent>(OnTransferAttempt);
        SubscribeLocalEvent<DrinkComponent, ConsumeDoAfterEvent>(OnDoAfter);
    }

    private FixedPoint2 DrinkVolume(EntityUid uid, DrinkComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return FixedPoint2.Zero;

        if (!_solutionContainer.TryGetSolution(uid, component.SolutionName, out var sol))
            return FixedPoint2.Zero;

        return sol.Volume;
    }

    public bool IsEmpty(EntityUid uid, DrinkComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return true;

        return DrinkVolume(uid, component) <= 0;
    }

    /// <summary>
    /// Get the total hydration factor contained in a drink's solution.
    /// </summary>
    public float TotalHydration(EntityUid uid, DrinkComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return 0f;

        if (!_solutionContainer.TryGetSolution(uid, comp.SolutionName, out var solution))
            return 0f;

        var total = 0f;
        foreach (var quantity in solution.Contents)
        {
            var reagent = _proto.Index<ReagentPrototype>(quantity.ReagentId);
            if (reagent.Metabolisms == null)
                continue;

            foreach ((var _, var entry) in reagent.Metabolisms)
            {
                foreach (var effect in entry.Effects)
                {
                    // ignores any effect conditions, just cares about how much it can hydrate
                    if (effect is SatiateThirst thirst)
                    {
                        total += thirst.HydrationFactor * quantity.Quantity.Float();
                    }
                }
            }
        }

        return total;
    }

    private void OnExamined(EntityUid uid, DrinkComponent component, ExaminedEvent args)
    {
        if (!component.Opened || !args.IsInDetailsRange || !component.Examinable)
            return;

        var color = IsEmpty(uid, component) ? "gray" : "yellow";
        var openedText =
            Loc.GetString(IsEmpty(uid, component) ? "drink-component-on-examine-is-empty" : "drink-component-on-examine-is-opened");
        args.Message.AddMarkup($"\n{Loc.GetString("drink-component-on-examine-details-text", ("colorName", color), ("text", openedText))}");
        if (!IsEmpty(uid, component))
        {
            if (TryComp<ExaminableSolutionComponent>(uid, out var comp))
            {
                //provide exact measurement for beakers
                args.Message.AddMarkup($" - {Loc.GetString("drink-component-on-examine-exact-volume", ("amount", DrinkVolume(uid, component)))}");
            }
            else
            {
                //general approximation
                var remainingString = (int) _solutionContainer.PercentFull(uid) switch
                {
                    100 => "drink-component-on-examine-is-full",
                    > 66 => "drink-component-on-examine-is-mostly-full",
                    > 33 => HalfEmptyOrHalfFull(args),
                    _ => "drink-component-on-examine-is-mostly-empty",
                };
                args.Message.AddMarkup($" - {Loc.GetString(remainingString)}");
            }
        }
    }

    private void SetOpen(EntityUid uid, bool opened = false, DrinkComponent? component = null)
    {
        if(!Resolve(uid, ref component))
            return;

        if (opened == component.Opened)
            return;

        component.Opened = opened;

        if (!_solutionContainer.TryGetSolution(uid, component.SolutionName, out _))
            return;

        if (EntityManager.TryGetComponent<AppearanceComponent>(uid, out var appearance))
        {
            _appearance.SetData(uid, DrinkCanStateVisual.Opened, opened, appearance);
        }
    }

    private void AfterInteract(EntityUid uid, DrinkComponent component, AfterInteractEvent args)
    {
        if (args.Handled || args.Target == null || !args.CanReach)
            return;

        args.Handled = TryDrink(args.User, args.Target.Value, component, uid);
    }

    private void OnUse(EntityUid uid, DrinkComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (!component.Opened)
        {
            //Do the opening stuff like playing the sounds.
            _audio.PlayPvs(_audio.GetSound(component.OpenSounds), args.User);

            SetOpen(uid, true, component);
            return;
        }

        args.Handled = TryDrink(args.User, args.User, component, uid);
    }

    private void HandleLand(EntityUid uid, DrinkComponent component, ref LandEvent args)
    {
        if (component.Pressurized &&
            !component.Opened &&
            _random.Prob(0.25f) &&
            _solutionContainer.TryGetSolution(uid, component.SolutionName, out var interactions))
        {
            component.Opened = true;
            UpdateAppearance(uid, component);

            var solution = _solutionContainer.SplitSolution(uid, interactions, interactions.Volume);
            _puddleSystem.TrySpillAt(uid, solution, out _);

            _audio.PlayPvs(_audio.GetSound(component.BurstSound), uid, AudioParams.Default.WithVolume(-4));
        }
    }

    private void OnDrinkInit(EntityUid uid, DrinkComponent component, ComponentInit args)
    {
        SetOpen(uid, component.DefaultToOpened, component);

        if (EntityManager.TryGetComponent(uid, out DrainableSolutionComponent? existingDrainable))
        {
            // Beakers have Drink component but they should use the existing Drainable
            component.SolutionName = existingDrainable.Solution;
        }
        else
        {
            _solutionContainer.EnsureSolution(uid, component.SolutionName);
        }

        UpdateAppearance(uid, component);

        if (TryComp(uid, out RefillableSolutionComponent? refillComp))
            refillComp.Solution = component.SolutionName;

        if (TryComp(uid, out DrainableSolutionComponent? drainComp))
            drainComp.Solution = component.SolutionName;
    }

    private void OnSolutionChange(EntityUid uid, DrinkComponent component, SolutionChangedEvent args)
    {
        UpdateAppearance(uid, component);
    }

    public void UpdateAppearance(EntityUid uid, DrinkComponent component)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance) ||
            !HasComp<SolutionContainerManagerComponent>(uid))
        {
            return;
        }

        var drainAvailable = DrinkVolume(uid, component);
        _appearance.SetData(uid, FoodVisuals.Visual, drainAvailable.Float(), appearance);
        _appearance.SetData(uid, DrinkCanStateVisual.Opened, component.Opened, appearance);
    }

    private void OnTransferAttempt(EntityUid uid, DrinkComponent component, SolutionTransferAttemptEvent args)
    {
        if (!component.Opened)
        {
            args.Cancel(Loc.GetString("drink-component-try-use-drink-not-open", ("owner", uid)));
        }
    }

    private bool TryDrink(EntityUid user, EntityUid target, DrinkComponent drink, EntityUid item)
    {
        if (!EntityManager.HasComponent<BodyComponent>(target))
            return false;

        if (!drink.Opened)
        {
            _popup.PopupEntity(Loc.GetString("drink-component-try-use-drink-not-open",
                ("owner", EntityManager.GetComponent<MetaDataComponent>(item).EntityName)), item, user);
            return true;
        }

        if (!_solutionContainer.TryGetSolution(item, drink.SolutionName, out var drinkSolution) ||
            drinkSolution.Volume <= 0)
        {
            _popup.PopupEntity(Loc.GetString("drink-component-try-use-drink-is-empty",
                ("entity", EntityManager.GetComponent<MetaDataComponent>(item).EntityName)), item, user);
            return true;
        }

        if (drinkSolution.Name == null)
            return false;

        if (_food.IsMouthBlocked(target, user))
            return true;

        if (!_interaction.InRangeUnobstructed(user, item, popup: true))
            return true;

        var forceDrink = user != target;

        if (forceDrink)
        {
            var userName = Identity.Entity(user, EntityManager);

            _popup.PopupEntity(Loc.GetString("drink-component-force-feed", ("user", userName)),
                user, target);

            // logging
            _adminLogger.Add(LogType.ForceFeed, LogImpact.Medium, $"{ToPrettyString(user):user} is forcing {ToPrettyString(target):target} to drink {ToPrettyString(item):drink} {SolutionContainerSystem.ToPrettyString(drinkSolution)}");
        }
        else
        {
            // log voluntary drinking
            _adminLogger.Add(LogType.Ingestion, LogImpact.Low, $"{ToPrettyString(target):target} is drinking {ToPrettyString(item):drink} {SolutionContainerSystem.ToPrettyString(drinkSolution)}");
        }

        var flavors = _flavorProfile.GetLocalizedFlavorsMessage(user, drinkSolution);

        var doAfterEventArgs = new DoAfterArgs(
            user,
            forceDrink ? drink.ForceFeedDelay : drink.Delay,
            new ConsumeDoAfterEvent(drinkSolution.Name, flavors),
            eventTarget: item,
            target: target,
            used: item)
        {
            BreakOnUserMove = forceDrink,
            BreakOnDamage = true,
            BreakOnTargetMove = forceDrink,
            MovementThreshold = 0.01f,
            DistanceThreshold = 1.0f,
            // Mice and the like can eat without hands.
            // TODO maybe set this based on some CanEatWithoutHands event or component?
            NeedHand = forceDrink,
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
        return true;
    }

    /// <summary>
    ///     Raised directed at a victim when someone has force fed them a drink.
    /// </summary>
    private void OnDoAfter(EntityUid uid, DrinkComponent component, ConsumeDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || component.Deleted)
            return;

        if (!TryComp<BodyComponent>(args.Target, out var body))
            return;

        if (!_solutionContainer.TryGetSolution(args.Used, args.Solution, out var solution))
            return;

        // TODO this should really be checked every tick.
        if (_food.IsMouthBlocked(args.Target.Value))
            return;

        // TODO this should really be checked every tick.
        if (!_interaction.InRangeUnobstructed(args.User, args.Target.Value))
            return;

        var transferAmount = FixedPoint2.Min(component.TransferAmount, solution.Volume);
        var drained = _solutionContainer.SplitSolution(uid, solution, transferAmount);
        var forceDrink = args.User != args.Target;

        args.Handled = true;
        if (transferAmount <= 0)
            return;

        if (!_body.TryGetBodyOrganComponents<StomachComponent>(args.Target.Value, out var stomachs, body))
        {
            _popup.PopupEntity(forceDrink ? Loc.GetString("drink-component-try-use-drink-cannot-drink-other") : Loc.GetString("drink-component-try-use-drink-had-enough"), args.Target.Value, args.User);

            if (HasComp<RefillableSolutionComponent>(args.Target.Value))
            {
                _puddleSystem.TrySpillAt(args.User, drained, out _);
                return;
            }

            _solutionContainer.Refill(args.Target.Value, solution, drained);
            return;
        }

        var firstStomach = stomachs.FirstOrNull(stomach => _stomach.CanTransferSolution(stomach.Comp.Owner, drained));

        //All stomachs are full or can't handle whatever solution we have.
        if (firstStomach == null)
        {
            _popup.PopupEntity(Loc.GetString("drink-component-try-use-drink-had-enough"), args.Target.Value, args.Target.Value);

            if (forceDrink)
            {
                _popup.PopupEntity(Loc.GetString("drink-component-try-use-drink-had-enough-other"), args.Target.Value, args.User);
                _puddleSystem.TrySpillAt(args.Target.Value, drained, out _);
            }
            else
                _solutionContainer.TryAddSolution(uid, solution, drained);

            return;
        }

        var flavors = args.FlavorMessage;

        if (forceDrink)
        {
            var targetName = Identity.Entity(args.Target.Value, EntityManager);
            var userName = Identity.Entity(args.User, EntityManager);

            _popup.PopupEntity(Loc.GetString("drink-component-force-feed-success", ("user", userName), ("flavors", flavors)), args.Target.Value, args.Target.Value);

            _popup.PopupEntity(
                Loc.GetString("drink-component-force-feed-success-user", ("target", targetName)),
                args.User, args.User);

            // log successful forced drinking
            _adminLogger.Add(LogType.ForceFeed, LogImpact.Medium, $"{ToPrettyString(uid):user} forced {ToPrettyString(args.User):target} to drink {ToPrettyString(uid):drink}");
        }
        else
        {
            _popup.PopupEntity(
                Loc.GetString("drink-component-try-use-drink-success-slurp-taste", ("flavors", flavors)), args.User,
                args.User);
            _popup.PopupEntity(
                Loc.GetString("drink-component-try-use-drink-success-slurp"), args.User, Filter.PvsExcept(args.User), true);

            // log successful voluntary drinking
            _adminLogger.Add(LogType.Ingestion, LogImpact.Low, $"{ToPrettyString(args.User):target} drank {ToPrettyString(uid):drink}");
        }

        _audio.PlayPvs(_audio.GetSound(component.UseSound), args.Target.Value, AudioParams.Default.WithVolume(-2f));

        _reaction.DoEntityReaction(args.Target.Value, solution, ReactionMethod.Ingestion);
        //TODO: Grab the stomach UIDs somehow without using Owner
        _stomach.TryTransferSolution(firstStomach.Value.Comp.Owner, drained, firstStomach.Value.Comp);

        var comp = EnsureComp<ForensicsComponent>(uid);
        if (TryComp<DnaComponent>(args.Target, out var dna))
            comp.DNAs.Add(dna.DNA);

        if (!forceDrink && solution.Volume > 0)
            args.Repeat = true;
    }

    private void AddDrinkVerb(EntityUid uid, DrinkComponent component, GetVerbsEvent<AlternativeVerb> ev)
    {
        if (uid == ev.User ||
            !ev.CanInteract ||
            !ev.CanAccess ||
            !EntityManager.TryGetComponent(ev.User, out BodyComponent? body) ||
            !_body.TryGetBodyOrganComponents<StomachComponent>(ev.User, out var stomachs, body))
            return;

        if (EntityManager.TryGetComponent<MobStateComponent>(uid, out var mobState) && _mobStateSystem.IsAlive(uid, mobState))
            return;

        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                TryDrink(ev.User, ev.User, component, uid);
            },
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/drink.svg.192dpi.png")),
            Text = Loc.GetString("drink-system-verb-drink"),
            Priority = 2
        };

        ev.Verbs.Add(verb);
    }

    // some see half empty, and others see half full
    private string HalfEmptyOrHalfFull(ExaminedEvent args)
    {
        string remainingString = "drink-component-on-examine-is-half-full";

        if (TryComp<MetaDataComponent>(args.Examiner, out var examiner) && examiner.EntityName.Length > 0
            && string.Compare(examiner.EntityName.Substring(0, 1), "m", StringComparison.InvariantCultureIgnoreCase) > 0)
            remainingString = "drink-component-on-examine-is-half-empty";

        return remainingString;
    }
}
