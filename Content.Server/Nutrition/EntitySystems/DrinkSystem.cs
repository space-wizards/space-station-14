using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.DoAfter;
using Content.Server.Fluids.Components;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Nutrition.Components;
using Content.Server.Popups;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Nutrition.EntitySystems
{
    [UsedImplicitly]
    public class DrinkSystem : EntitySystem
    {
        [Dependency] private readonly FoodSystem _foodSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly BodySystem _bodySystem = default!;
        [Dependency] private readonly StomachSystem _stomachSystem = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly SharedAdminLogSystem _logSystem = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly SpillableSystem _spillableSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DrinkComponent, SolutionChangedEvent>(OnSolutionChange);
            SubscribeLocalEvent<DrinkComponent, ComponentInit>(OnDrinkInit);
            SubscribeLocalEvent<DrinkComponent, LandEvent>(HandleLand);
            SubscribeLocalEvent<DrinkComponent, UseInHandEvent>(OnUse);
            SubscribeLocalEvent<DrinkComponent, HandDeselectedEvent>(OnDrinkDeselected);
            SubscribeLocalEvent<DrinkComponent, AfterInteractEvent>(AfterInteract);
            SubscribeLocalEvent<DrinkComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<SharedBodyComponent, ForceDrinkEvent>(OnForceDrink);
            SubscribeLocalEvent<ForceDrinkCancelledEvent>(OnForceDrinkCancelled);
        }

        /// <summary>
        ///     If the user is currently forcing someone do drink, this cancels the attempt if they swap hands or
        ///     otherwise loose the item. Prevents force-feeding dual-wielding.
        /// </summary>
        private void OnDrinkDeselected(EntityUid uid, DrinkComponent component, HandDeselectedEvent args)
        {
            if (component.CancelToken != null)
            {
                component.CancelToken.Cancel();
                component.CancelToken = null;
            }
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
            {
                return;
            }

            var color = IsEmpty(uid, component) ? "gray" : "yellow";
            var openedText =
                Loc.GetString(IsEmpty(uid, component) ? "drink-component-on-examine-is-empty" : "drink-component-on-examine-is-opened");
            args.Message.AddMarkup($"\n{Loc.GetString("drink-component-on-examine-details-text", ("colorName", color), ("text", openedText))}");
        }

        private void SetOpen(EntityUid uid, bool opened = false, DrinkComponent? component = null)
        {
            if(!Resolve(uid, ref component))
                return;

            if (opened == component.Opened)
                return;

            component.Opened = opened;

            if (!_solutionContainerSystem.TryGetSolution(uid, component.SolutionName, out _))
            {
                return;
            }

            if (EntityManager.TryGetComponent<AppearanceComponent>(uid, out var appearance))
            {
                appearance.SetData(DrinkCanStateVisual.Opened, opened);
            }

            if (opened)
            {
                EntityManager.EnsureComponent<RefillableSolutionComponent>(uid).Solution= component.SolutionName;
                EntityManager.EnsureComponent<DrainableSolutionComponent>(uid).Solution= component.SolutionName;
            }
            else
            {
                EntityManager.RemoveComponent<RefillableSolutionComponent>(uid);
                EntityManager.RemoveComponent<DrainableSolutionComponent>(uid);
            }

        }

        private void AfterInteract(EntityUid uid, DrinkComponent component, AfterInteractEvent args)
        {
            if (args.Handled || args.Target == null)
                return;

            if (!_actionBlockerSystem.CanInteract(args.User) || !_actionBlockerSystem.CanUse(args.User))
                return;

            if (!args.User.InRangeUnobstructed(uid, popup: true))
            {
                args.Handled = true;
                return;
            }

            if (args.User == args.Target)
            {
                args.Handled = TryUseDrink(uid, args.User);
                return;
            }

            if (!args.User.InRangeUnobstructed(args.Target.Value, popup: true))
            {
                args.Handled = true;
                return;
            }

            if (args.User == args.Target)
                args.Handled = TryUseDrink(uid, args.User, component);
            else
                args.Handled = TryForceDrink(uid, args.User, args.Target.Value, component);
        }

        private void OnUse(EntityUid uid, DrinkComponent component, UseInHandEvent args)
        {
            if (args.Handled) return;

            if (!_actionBlockerSystem.CanInteract(args.User) || !_actionBlockerSystem.CanUse(args.User))
                return;

            if (!args.User.InRangeUnobstructed(uid, popup: true))
            {
                args.Handled = true;
                return;
            }

            if (!component.Opened)
            {
                //Do the opening stuff like playing the sounds.
                SoundSystem.Play(Filter.Pvs(args.User), component.OpenSounds.GetSound(), args.User, AudioParams.Default);

                SetOpen(uid, true, component);
                return;
            }

            if (_solutionContainerSystem.DrainAvailable(uid) <= 0)
            {
                args.User.PopupMessage(Loc.GetString("drink-component-on-use-is-empty", ("owner", uid)));
                return;
            }

            args.Handled = TryUseDrink(uid, args.User, component);
        }

        private void HandleLand(EntityUid uid, DrinkComponent component, LandEvent args)
        {
            if (component.Pressurized &&
                !component.Opened &&
                _random.Prob(0.25f) &&
                _solutionContainerSystem.TryGetDrainableSolution(uid, out var interactions))
            {
                component.Opened = true;
                UpdateAppearance(component);

                var solution = _solutionContainerSystem.Drain(uid, interactions, interactions.DrainAvailable);
                _spillableSystem.SpillAt(uid, solution, "PuddleSmear");

                SoundSystem.Play(Filter.Pvs(uid), component.BurstSound.GetSound(), uid, AudioParams.Default.WithVolume(-4));
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
            appearance.SetData(FoodVisuals.Visual, drainAvailable.Float());
            appearance.SetData(DrinkCanStateVisual.Opened, component.Opened);
        }

        /// <summary>
        ///     Attempt to drink some of a drink. Returns true if any interaction took place, including generation of
        ///     pop-up messages.
        /// </summary>
        private bool TryUseDrink(EntityUid uid, EntityUid userUid, DrinkComponent? drink = null)
        {
            if (!Resolve(uid, ref drink))
                return false;

            // if currently being used to force-feed, cancel that action.
            if (drink.CancelToken != null)
            {
                drink.CancelToken.Cancel();
                drink.CancelToken = null;
                return true;
            }

            if (!drink.Opened)
            {
                _popupSystem.PopupEntity(Loc.GetString("drink-component-try-use-drink-not-open",
                    ("owner", Name: EntityManager.GetComponent<MetaDataComponent>(drink.Owner).EntityName)), uid, Filter.Entities(userUid));
                return true;
            }

            if (!EntityManager.TryGetComponent(userUid, out SharedBodyComponent? body))
                return false;

            if (!_solutionContainerSystem.TryGetDrainableSolution((drink).Owner, out var drinkSolution) ||
                drinkSolution.DrainAvailable <= 0)
            {
                _popupSystem.PopupEntity(Loc.GetString("drink-component-try-use-drink-is-empty",
                    ("entity", Name: EntityManager.GetComponent<MetaDataComponent>(drink.Owner).EntityName)), uid, Filter.Entities(userUid));
                return true;
            }

            if (!_bodySystem.TryGetComponentsOnMechanisms<StomachComponent>(userUid, out var stomachs, body))
            {
                _popupSystem.PopupEntity(Loc.GetString("drink-component-try-use-drink-cannot-drink"),
                    userUid, Filter.Entities(userUid));
                return true;
            }

            if (_foodSystem.IsMouthBlocked(userUid, userUid))
                return true;

            var transferAmount = FixedPoint2.Min(drink.TransferAmount, drinkSolution.DrainAvailable);
            var drain = _solutionContainerSystem.Drain(uid, drinkSolution, transferAmount);
            var firstStomach = stomachs.FirstOrNull(
                stomach => _stomachSystem.CanTransferSolution((stomach.Comp).Owner, drain));

            // All stomach are full or can't handle whatever solution we have.
            if (firstStomach == null)
            {
                _popupSystem.PopupEntity(Loc.GetString("drink-component-try-use-drink-had-enough"),
                    userUid, Filter.Entities(userUid));

                if (EntityManager.HasComponent<RefillableSolutionComponent>(uid))
                {
                    _spillableSystem.SpillAt(userUid, drain, "PuddleSmear");
                    return true;
                }

                _solutionContainerSystem.Refill(uid, drinkSolution, drain);
                return true;
            }

            SoundSystem.Play(Filter.Pvs(userUid), drink.UseSound.GetSound(), userUid,
                AudioParams.Default.WithVolume(-2f));

            _popupSystem.PopupEntity(Loc.GetString("drink-component-try-use-drink-success-slurp"), userUid,
                Filter.Pvs(userUid));

            drain.DoEntityReaction(userUid, ReactionMethod.Ingestion);
            _stomachSystem.TryTransferSolution((firstStomach.Value.Comp).Owner, drain, firstStomach.Value.Comp);

            return true;
        }

        /// <summary>
        ///     Attempt to force someone else to drink some of a drink. Returns true if any interaction took place,
        ///     including generation of pop-up messages.
        /// </summary>
        private bool TryForceDrink(EntityUid uid, EntityUid userUid, EntityUid targetUid,
            DrinkComponent? drink = null)
        {
            if (!Resolve(uid, ref drink))
                return false;

            // cannot stack do-afters
            if (drink.CancelToken != null)
            {
                drink.CancelToken.Cancel();
                drink.CancelToken = null;
                return true;
            }

            if (!EntityManager.HasComponent<SharedBodyComponent>(targetUid))
                return false;

            if (!drink.Opened)
            {
                _popupSystem.PopupEntity(Loc.GetString("drink-component-try-use-drink-not-open",
                    ("owner", Name: EntityManager.GetComponent<MetaDataComponent>(drink.Owner).EntityName)), uid, Filter.Entities(userUid));
                return true;
            }

            if (!_solutionContainerSystem.TryGetDrainableSolution(uid, out var drinkSolution) ||
                drinkSolution.DrainAvailable <= 0)
            {
                _popupSystem.PopupEntity(Loc.GetString("drink-component-try-use-drink-is-empty",
                    ("entity", Name: EntityManager.GetComponent<MetaDataComponent>(drink.Owner).EntityName)), uid, Filter.Entities(userUid));
                return true;
            }

            if (_foodSystem.IsMouthBlocked(targetUid, userUid))
                return true;

            EntityManager.TryGetComponent(userUid, out MetaDataComponent? meta);
            var userName = meta?.EntityName ?? string.Empty;

            _popupSystem.PopupEntity(Loc.GetString("drink-component-force-feed", ("user", userName)),
                userUid, Filter.Entities(targetUid));

            drink.CancelToken = new();
            _doAfterSystem.DoAfter(new DoAfterEventArgs(userUid, drink.ForceFeedDelay, drink.CancelToken.Token, targetUid)
            {
                BreakOnUserMove = true,
                BreakOnDamage = true,
                BreakOnStun = true,
                BreakOnTargetMove = true,
                MovementThreshold = 1.0f,
                TargetFinishedEvent = new ForceDrinkEvent(userUid, drink, drinkSolution),
                BroadcastCancelledEvent = new ForceDrinkCancelledEvent(drink),
            });

            // logging
            _logSystem.Add(LogType.ForceFeed, LogImpact.Medium, $"{ToPrettyString(userUid):user} is forcing {ToPrettyString(targetUid):target} to drink {ToPrettyString(uid):drink} {SolutionContainerSystem.ToPrettyString(drinkSolution):solution}");

            return true;
        }

        /// <summary>
        ///     Raised directed at a victim when someone has force fed them a drink.
        /// </summary>
        private void OnForceDrink(EntityUid uid, SharedBodyComponent body, ForceDrinkEvent args)
        {
            if (args.Drink.Deleted)
                return;

            args.Drink.CancelToken = null;
            var transferAmount = FixedPoint2.Min(args.Drink.TransferAmount, args.DrinkSolution.DrainAvailable);
            var drained = _solutionContainerSystem.Drain((args.Drink).Owner, args.DrinkSolution, transferAmount);

            if (!_bodySystem.TryGetComponentsOnMechanisms<StomachComponent>(uid, out var stomachs, body))
            {
                _popupSystem.PopupEntity(Loc.GetString("drink-component-try-use-drink-cannot-drink-other"),
                    uid, Filter.Entities(args.User));

                _spillableSystem.SpillAt(uid, drained, "PuddleSmear");
                return;
            }

            var firstStomach = stomachs.FirstOrNull(
                stomach => _stomachSystem.CanTransferSolution((stomach.Comp).Owner, drained));

            // All stomach are full or can't handle whatever solution we have.
            if (firstStomach == null)
            {
                _popupSystem.PopupEntity(Loc.GetString("drink-component-try-use-drink-had-enough-other"),
                    uid, Filter.Entities(args.User));

                _spillableSystem.SpillAt(uid, drained, "PuddleSmear");
                return;
            }

            EntityManager.TryGetComponent(uid, out MetaDataComponent? targetMeta);
            var targetName = targetMeta?.EntityName ?? string.Empty;

            EntityManager.TryGetComponent(args.User, out MetaDataComponent? userMeta);
            var userName = userMeta?.EntityName ?? string.Empty;

            _popupSystem.PopupEntity(Loc.GetString("drink-component-force-feed-success", ("user", userName)),
                uid, Filter.Entities(uid));

            _popupSystem.PopupEntity(Loc.GetString("drink-component-force-feed-success-user", ("target", targetName)),
                args.User, Filter.Entities(args.User));

            SoundSystem.Play(Filter.Pvs(uid), args.Drink.UseSound.GetSound(), uid, AudioParams.Default.WithVolume(-2f));

            drained.DoEntityReaction(uid, ReactionMethod.Ingestion);
            _stomachSystem.TryTransferSolution((firstStomach.Value.Comp).Owner, drained, firstStomach.Value.Comp);
        }

        private void OnForceDrinkCancelled(ForceDrinkCancelledEvent args)
        {
            args.Drink.CancelToken = null;
        }
    }
}
