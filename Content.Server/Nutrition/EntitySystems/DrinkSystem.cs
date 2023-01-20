using System.Threading;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.DoAfter;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Nutrition.Components;
using Content.Server.Popups;
using Content.Shared.Administration.Logs;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Nutrition.EntitySystems
{
    [UsedImplicitly]
    public sealed class DrinkSystem : EntitySystem
    {
        [Dependency] private readonly FoodSystem _foodSystem = default!;
        [Dependency] private readonly FlavorProfileSystem _flavorProfileSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly BodySystem _bodySystem = default!;
        [Dependency] private readonly StomachSystem _stomachSystem = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
        [Dependency] private readonly SpillableSystem _spillableSystem = default!;
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DrinkComponent, SolutionChangedEvent>(OnSolutionChange);
            SubscribeLocalEvent<DrinkComponent, ComponentInit>(OnDrinkInit);
            SubscribeLocalEvent<DrinkComponent, LandEvent>(HandleLand);
            SubscribeLocalEvent<DrinkComponent, UseInHandEvent>(OnUse);
            SubscribeLocalEvent<DrinkComponent, AfterInteractEvent>(AfterInteract);
            SubscribeLocalEvent<DrinkComponent, GetVerbsEvent<AlternativeVerb>>(AddDrinkVerb);
            SubscribeLocalEvent<DrinkComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<DrinkComponent, SolutionTransferAttemptEvent>(OnTransferAttempt);
            SubscribeLocalEvent<BodyComponent, DrinkEvent>(OnDrink);
            SubscribeLocalEvent<DrinkCancelledEvent>(OnDrinkCancelled);
        }

        public bool IsEmpty(EntityUid uid, DrinkComponent? component = null)
        {
            if(!Resolve(uid, ref component))
                return true;

            return _solutionContainerSystem.DrainAvailable(uid) <= 0;
        }

        private void OnExamined(EntityUid uid, DrinkComponent component, ExaminedEvent args)
        {
            if (!component.Opened || !args.IsInDetailsRange)
                return;

            var color = IsEmpty(uid, component) ? "gray" : "yellow";
            var openedText =
                Loc.GetString(IsEmpty(uid, component) ? "drink-component-on-examine-is-empty" : "drink-component-on-examine-is-opened");
            args.Message.AddMarkup($"\n{Loc.GetString("drink-component-on-examine-details-text", ("colorName", color), ("text", openedText))}");
            if (!IsEmpty(uid, component))
            {
                if (TryComp<ExaminableSolutionComponent>(component.Owner, out var comp))
                {
                    //provide exact measurement for beakers
                    args.Message.AddMarkup($" - {Loc.GetString("drink-component-on-examine-exact-volume", ("amount", _solutionContainerSystem.DrainAvailable(uid)))}");
                }
                else
                {
                    //general approximation
                    var remainingString = (int) _solutionContainerSystem.PercentFull(uid) switch
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

            if (!_solutionContainerSystem.TryGetSolution(uid, component.SolutionName, out _))
                return;

            if (EntityManager.TryGetComponent<AppearanceComponent>(uid, out var appearance))
            {
                _appearanceSystem.SetData(uid, DrinkCanStateVisual.Opened, opened, appearance);
            }
        }

        private void AfterInteract(EntityUid uid, DrinkComponent component, AfterInteractEvent args)
        {
            if (args.Handled || args.Target == null || !args.CanReach)
                return;

            args.Handled = TryDrink(args.User, args.Target.Value, component);
        }

        private void OnUse(EntityUid uid, DrinkComponent component, UseInHandEvent args)
        {
            if (args.Handled) return;

            if (!component.Opened)
            {
                //Do the opening stuff like playing the sounds.
                _audio.PlayPvs(_audio.GetSound(component.OpenSounds), args.User);

                SetOpen(uid, true, component);
                return;
            }

            args.Handled = TryDrink(args.User, args.User, component);
        }

        private void HandleLand(EntityUid uid, DrinkComponent component, ref LandEvent args)
        {
            if (component.Pressurized &&
                !component.Opened &&
                _random.Prob(0.25f) &&
                _solutionContainerSystem.TryGetDrainableSolution(uid, out var interactions))
            {
                component.Opened = true;
                UpdateAppearance(component);

                var solution = _solutionContainerSystem.Drain(uid, interactions, interactions.Volume);
                _spillableSystem.SpillAt(uid, solution, "PuddleSmear");

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
                _solutionContainerSystem.EnsureSolution(uid, component.SolutionName);
            }

            UpdateAppearance(component);

            if (TryComp(uid, out RefillableSolutionComponent? refillComp))
                refillComp.Solution = component.SolutionName;

            if (TryComp(uid, out DrainableSolutionComponent? drainComp))
                drainComp.Solution = component.SolutionName;
        }

        private void OnSolutionChange(EntityUid uid, DrinkComponent component, SolutionChangedEvent args)
        {
            UpdateAppearance(component);
        }

        public void UpdateAppearance(DrinkComponent component)
        {
            if (!EntityManager.TryGetComponent((component).Owner, out AppearanceComponent? appearance) ||
                !EntityManager.HasComponent<SolutionContainerManagerComponent>((component).Owner))
            {
                return;
            }

            var drainAvailable = _solutionContainerSystem.DrainAvailable((component).Owner);
            _appearanceSystem.SetData(component.Owner, FoodVisuals.Visual, drainAvailable.Float(), appearance);
            _appearanceSystem.SetData(component.Owner, DrinkCanStateVisual.Opened, component.Opened, appearance);
        }

        private void OnTransferAttempt(EntityUid uid, DrinkComponent component, SolutionTransferAttemptEvent args)
        {
            if (!component.Opened)
            {
                args.Cancel(Loc.GetString("drink-component-try-use-drink-not-open",
                    ("owner", EntityManager.GetComponent<MetaDataComponent>(component.Owner).EntityName)));
            }
        }

        private bool TryDrink(EntityUid user, EntityUid target, DrinkComponent drink)
        {
            // cannot stack do-afters
            if (drink.CancelToken != null)
            {
                return true;
            }

            if (!EntityManager.HasComponent<BodyComponent>(target))
                return false;

            if (!drink.Opened)
            {
                _popupSystem.PopupEntity(Loc.GetString("drink-component-try-use-drink-not-open",
                    ("owner", EntityManager.GetComponent<MetaDataComponent>(drink.Owner).EntityName)), drink.Owner, user);
                return true;
            }

            if (!_solutionContainerSystem.TryGetDrainableSolution(drink.Owner, out var drinkSolution) ||
                drinkSolution.Volume <= 0)
            {
                _popupSystem.PopupEntity(Loc.GetString("drink-component-try-use-drink-is-empty",
                    ("entity", EntityManager.GetComponent<MetaDataComponent>(drink.Owner).EntityName)), drink.Owner, user);
                return true;
            }

            if (_foodSystem.IsMouthBlocked(target, user))
                return true;

            if (!_interactionSystem.InRangeUnobstructed(user, drink.Owner, popup: true))
                return true;

            var forceDrink = user != target;

            if (forceDrink)
            {
                var userName = Identity.Entity(user, EntityManager);

                _popupSystem.PopupEntity(Loc.GetString("drink-component-force-feed", ("user", userName)),
                    user, target);

                // logging
                _adminLogger.Add(LogType.ForceFeed, LogImpact.Medium, $"{ToPrettyString(user):user} is forcing {ToPrettyString(target):target} to drink {ToPrettyString(drink.Owner):drink} {SolutionContainerSystem.ToPrettyString(drinkSolution)}");
            }
            else
            {
                // log voluntary drinking
                _adminLogger.Add(LogType.Ingestion, LogImpact.Low, $"{ToPrettyString(target):target} is drinking {ToPrettyString(drink.Owner):drink} {SolutionContainerSystem.ToPrettyString(drinkSolution)}");
            }

            drink.CancelToken = new CancellationTokenSource();
            var moveBreak = user != target;

            var flavors = _flavorProfileSystem.GetLocalizedFlavorsMessage(user, drinkSolution);

            _doAfterSystem.DoAfter(new DoAfterEventArgs(user, forceDrink ? drink.ForceFeedDelay : drink.Delay, drink.CancelToken.Token, target, drink.Owner)
            {
                BreakOnUserMove = moveBreak,
                BreakOnDamage = true,
                BreakOnStun = true,
                BreakOnTargetMove = moveBreak,
                MovementThreshold = 0.01f,
                DistanceThreshold = 1.0f,
                TargetFinishedEvent = new DrinkEvent(user, drink, drinkSolution, flavors),
                BroadcastCancelledEvent = new DrinkCancelledEvent(drink),
                NeedHand = true,
            });

            return true;
        }

        /// <summary>
        ///     Raised directed at a victim when someone has force fed them a drink.
        /// </summary>
        private void OnDrink(EntityUid uid, BodyComponent body, DrinkEvent args)
        {
            if (args.Drink.Deleted)
                return;

            args.Drink.CancelToken = null;
            var transferAmount = FixedPoint2.Min(args.Drink.TransferAmount, args.DrinkSolution.Volume);
            var drained = _solutionContainerSystem.Drain(args.Drink.Owner, args.DrinkSolution, transferAmount);

            var forceDrink = uid != args.User;

            if (!_bodySystem.TryGetBodyOrganComponents<StomachComponent>(uid, out var stomachs, body))
            {
                _popupSystem.PopupEntity(
                    forceDrink ?
                        Loc.GetString("drink-component-try-use-drink-cannot-drink-other") :
                        Loc.GetString("drink-component-try-use-drink-had-enough"),
                    uid, args.User);

                if (EntityManager.HasComponent<RefillableSolutionComponent>(uid))
                {
                    _spillableSystem.SpillAt(args.User, drained, "PuddleSmear");
                    return;
                }

                _solutionContainerSystem.Refill(uid, args.DrinkSolution, drained);
                return;
            }

            var firstStomach = stomachs.FirstOrNull(
                stomach => _stomachSystem.CanTransferSolution((stomach.Comp).Owner, drained));

            // All stomach are full or can't handle whatever solution we have.
            if (firstStomach == null)
            {
                _popupSystem.PopupEntity(Loc.GetString("drink-component-try-use-drink-had-enough"),
                    uid, uid);

                if (forceDrink)
                {
                    _popupSystem.PopupEntity(Loc.GetString("drink-component-try-use-drink-had-enough-other"),
                        uid, args.User);
                    _spillableSystem.SpillAt(uid, drained, "PuddleSmear");
                }
                else
                {
                    _solutionContainerSystem.TryAddSolution(args.Drink.Owner, args.DrinkSolution, drained);
                }

                return;
            }

            var flavors = args.FlavorMessage;

            if (forceDrink)
            {
                var targetName = Identity.Entity(uid, EntityManager);
                var userName = Identity.Entity(args.User, EntityManager);

                _popupSystem.PopupEntity(
                    Loc.GetString("drink-component-force-feed-success", ("user", userName), ("flavors", flavors)), uid, uid);

                _popupSystem.PopupEntity(
                    Loc.GetString("drink-component-force-feed-success-user", ("target", targetName)),
                    args.User, args.User);

                // log successful forced drinking
                _adminLogger.Add(LogType.ForceFeed, LogImpact.Medium, $"{ToPrettyString(uid):user} forced {ToPrettyString(args.User):target} to drink {ToPrettyString(args.Drink.Owner):drink}");
            }
            else
            {
                _popupSystem.PopupEntity(
                    Loc.GetString("drink-component-try-use-drink-success-slurp-taste", ("flavors", flavors)), args.User,
                    args.User);
                _popupSystem.PopupEntity(
                    Loc.GetString("drink-component-try-use-drink-success-slurp"), args.User, Filter.PvsExcept(args.User), true);

                // log successful voluntary drinking
                _adminLogger.Add(LogType.Ingestion, LogImpact.Low, $"{ToPrettyString(args.User):target} drank {ToPrettyString(args.Drink.Owner):drink}");
            }

            _audio.PlayPvs(_audio.GetSound(args.Drink.UseSound), uid, AudioParams.Default.WithVolume(-2f));

            drained.DoEntityReaction(uid, ReactionMethod.Ingestion);
            _stomachSystem.TryTransferSolution(firstStomach.Value.Comp.Owner, drained, firstStomach.Value.Comp);
        }

        private static void OnDrinkCancelled(DrinkCancelledEvent args)
        {
            args.Drink.CancelToken = null;
        }

        private void AddDrinkVerb(EntityUid uid, DrinkComponent component, GetVerbsEvent<AlternativeVerb> ev)
        {
            if (component.CancelToken != null)
                return;

            if (uid == ev.User ||
                !ev.CanInteract ||
                !ev.CanAccess ||
                !EntityManager.TryGetComponent(ev.User, out BodyComponent? body) ||
                !_bodySystem.TryGetBodyOrganComponents<StomachComponent>(ev.User, out var stomachs, body))
                return;

            if (EntityManager.TryGetComponent<MobStateComponent>(uid, out var mobState) && _mobStateSystem.IsAlive(uid, mobState))
                return;

            AlternativeVerb verb = new()
            {
                Act = () =>
                {
                    TryDrink(ev.User, ev.User, component);
                },
                IconTexture = "/Textures/Interface/VerbIcons/drink.svg.192dpi.png",
                Text = Loc.GetString("drink-system-verb-drink"),
                Priority = -1
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
}
