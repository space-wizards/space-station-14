using System.Linq;
using Content.Server.Body.Behavior;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Fluids.Components;
using Content.Server.Nutrition.Components;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Nutrition.EntitySystems
{
    [UsedImplicitly]
    public class DrinkSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DrinkComponent, SolutionChangedEvent>(OnSolutionChange);
            SubscribeLocalEvent<DrinkComponent, ComponentInit>(OnDrinkInit);
            SubscribeLocalEvent<DrinkComponent, LandEvent>(HandleLand);
            SubscribeLocalEvent<DrinkComponent, UseInHandEvent>(OnUse);
            SubscribeLocalEvent<DrinkComponent, AfterInteractEvent>(AfterInteract);
            SubscribeLocalEvent<DrinkComponent, ExaminedEvent>(OnExamined);
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
            if (args.Handled)
                return;

            if (args.Target == null)
                return;

            if (TryUseDrink(uid, args.User, args.Target, true, component))
                args.Handled = true;
        }

        private void OnUse(EntityUid uid, DrinkComponent component, UseInHandEvent args)
        {
            if (args.Handled) return;
            if (!component.Opened)
            {
                //Do the opening stuff like playing the sounds.
                SoundSystem.Play(Filter.Pvs(args.User), component.OpenSounds.GetSound(), args.User, AudioParams.Default);

                SetOpen(uid, true, component);
                return;
            }

            if (_solutionContainerSystem.DrainAvailable(uid) <= 0)
            {
                args.User.PopupMessage(Loc.GetString("drink-component-on-use-is-empty", ("owner", EntityManager.GetEntity(uid))));
                return;
            }

            if (TryUseDrink(uid, args.User, args.User, false, component))
                args.Handled = true;
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

                var entity = EntityManager.GetEntity(uid);

                var solution = _solutionContainerSystem.Drain(uid, interactions, interactions.DrainAvailable);
                solution.SpillAt(entity, "PuddleSmear");

                SoundSystem.Play(Filter.Pvs(entity), component.BurstSound.GetSound(), entity, AudioParams.Default.WithVolume(-4));
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
            if (!EntityManager.TryGetComponent(component.OwnerUid, out AppearanceComponent? appearance) ||
                !EntityManager.HasComponent<SolutionContainerManagerComponent>(component.OwnerUid))
            {
                return;
            }

            var drainAvailable = _solutionContainerSystem.DrainAvailable(component.OwnerUid);
            appearance.SetData(FoodVisuals.Visual, drainAvailable.Float());
            appearance.SetData(DrinkCanStateVisual.Opened, component.Opened);
        }

        private bool TryUseDrink(EntityUid uid, IEntity user, IEntity target, bool forced, DrinkComponent? component = null)
        {
            if(!Resolve(uid, ref component))
                return false;

            var owner = component.Owner;

            if (!component.Opened)
            {
                target.PopupMessage(Loc.GetString("drink-component-try-use-drink-not-open", ("owner", owner)));
                return false;
            }

            if (!_solutionContainerSystem.TryGetDrainableSolution(component.OwnerUid, out var interactions) ||
                interactions.DrainAvailable <= 0)
            {
                if (!forced)
                {
                    target.PopupMessage(Loc.GetString("drink-component-try-use-drink-is-empty", ("entity", owner)));
                }

                return false;
            }

            if (!EntityManager.TryGetComponent(target.Uid, out SharedBodyComponent? body) ||
                !body.TryGetMechanismBehaviors<StomachBehavior>(out var stomachs))
            {
                target.PopupMessage(Loc.GetString("drink-component-try-use-drink-cannot-drink", ("owner", owner)));
                return false;
            }

            if (user != target && !user.InRangeUnobstructed(target, popup: true))
                return false;

            var transferAmount = FixedPoint2.Min(component.TransferAmount, interactions.DrainAvailable);
            var drain = _solutionContainerSystem.Drain(owner.Uid, interactions, transferAmount);
            var firstStomach = stomachs.FirstOrDefault(stomach => stomach.CanTransferSolution(drain));

            // All stomach are full or can't handle whatever solution we have.
            if (firstStomach == null)
            {
                target.PopupMessage(Loc.GetString("drink-component-try-use-drink-had-enough", ("owner", owner)));

                if (EntityManager.HasComponent<RefillableSolutionComponent>(uid))
                {
                    drain.SpillAt(target, "PuddleSmear");
                    return false;
                }

                _solutionContainerSystem.Refill(owner.Uid, interactions, drain);
                return false;
            }

            SoundSystem.Play(Filter.Pvs(target), component.UseSound.GetSound(), target, AudioParams.Default.WithVolume(-2f));

            target.PopupMessage(Loc.GetString("drink-component-try-use-drink-success-slurp"));

            // TODO: Account for partial transfer.

            drain.DoEntityReaction(target, ReactionMethod.Ingestion);

            firstStomach.TryTransferSolution(drain);

            return true;
        }
    }
}
