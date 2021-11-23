using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Server.Nutrition.Components;
using Content.Server.Popups;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Player;
using System.Collections.Generic;
using System.Linq;

namespace Content.Server.Nutrition.EntitySystems
{
    /// <summary>
    /// Handles feeding attempts both on yourself and on the target.
    /// </summary>
    internal class FoodSystem : EntitySystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly BodySystem _bodySystem = default!;
        [Dependency] private readonly StomachSystem _stomachSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly UtensilSystem _utensilSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<FoodComponent, UseInHandEvent>(OnUseFoodInHand);
            SubscribeLocalEvent<FoodComponent, AfterInteractEvent>(OnFeedFood);
            SubscribeLocalEvent<FoodComponent, GetInteractionVerbsEvent>(AddEatVerb);
        }

        /// <summary>
        /// Eat item
        /// </summary>
        private void OnUseFoodInHand(EntityUid uid, FoodComponent foodComponent, UseInHandEvent ev)
        {
            if (ev.Handled)
                return;

            if (TryUseFood(uid, ev.UserUid, ev.UserUid))
                ev.Handled = true;
        }

        /// <summary>
        /// Feed someone else
        /// </summary>
        private void OnFeedFood(EntityUid uid, FoodComponent foodComponent, AfterInteractEvent ev)
        {
            if (ev.Handled || ev.Target == null)
                return;

            if (TryUseFood(uid, ev.UserUid, ev.Target.Uid))
                ev.Handled = true;
        }

        /// <summary>
        /// Tries to feed specified target
        /// </summary>
        /// <param name="uid">Food entity.</param>
        /// <param name="userUid">Feeding initiator.</param>
        /// <param name="targetUid">Feeding target.</param>
        /// <returns>True if the portion of food was consumed</returns>
        public bool TryUseFood(EntityUid uid, EntityUid userUid, EntityUid targetUid, FoodComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return false;

            if (!_solutionContainerSystem.TryGetSolution(uid, component.SolutionName, out var solution))
                return false;

            if (component.UsesRemaining <= 0)
            {
                _popupSystem.PopupEntity(Loc.GetString("food-system-try-use-food-is-empty", ("entity", EntityManager.GetEntity(uid))), userUid, Filter.Entities(userUid));
                DeleteAndSpawnTrash(userUid, component);
                return false;
            }

            if (!EntityManager.TryGetComponent(targetUid, out SharedBodyComponent ? body) ||
                !_bodySystem.TryGetComponentsOnMechanisms<StomachComponent>(targetUid, out var stomachs, body))
                return false;

            var usedUtensils = new List<UtensilComponent>();

            //Not blocking eating itself if "required" filed is not set (allows usage of multiple types of utensils)
            //TODO: maybe a chance to spill soup on eating without spoon?!
            if (component.Utensil != UtensilType.None)
            {
                if (EntityManager.TryGetComponent(userUid, out HandsComponent? hands))
                {
                    var usedTypes = UtensilType.None;

                    foreach (var item in hands.GetAllHeldItems())
                    {
                        // Is utensil?
                        if (!item.Owner.TryGetComponent(out UtensilComponent? utensil))
                            continue;

                        if ((utensil.Types & component.Utensil) != 0 && // Acceptable type?
                            (usedTypes & utensil.Types) != utensil.Types) // Type is not used already? (removes usage of identical utensils)
                        {
                            // Add to used list
                            usedTypes |= utensil.Types;
                            usedUtensils.Add(utensil);
                        }
                    }

                    // If "required" field is set, try to block eating without proper utensils used
                    if (component.UtensilRequired && (usedTypes & component.Utensil) != component.Utensil)
                    {
                        _popupSystem.PopupEntity(Loc.GetString("food-you-need-to-hold-utensil", ("utensil", component.Utensil ^ usedTypes)), userUid, Filter.Entities(userUid));
                        return false;
                    }
                }
            }

            if (userUid != targetUid && !userUid.InRangeUnobstructed(targetUid, popup: true))
                return false;

            var transferAmount = component.TransferAmount != null ? FixedPoint2.Min((FixedPoint2) component.TransferAmount, solution.CurrentVolume) : solution.CurrentVolume;
            var split = _solutionContainerSystem.SplitSolution(uid, solution, transferAmount);
            var firstStomach = stomachs.FirstOrDefault(stomach => _stomachSystem.CanTransferSolution(stomach.OwnerUid, split));

            if (firstStomach == null)
            {
                _solutionContainerSystem.TryAddSolution(uid, solution, split);
                _popupSystem.PopupEntity(Loc.GetString("food-system-you-cannot-eat-any-more"), targetUid, Filter.Entities(targetUid));
                return false;
            }

            // TODO: Account for partial transfer.
            split.DoEntityReaction(targetUid, ReactionMethod.Ingestion);
            _stomachSystem.TryTransferSolution(firstStomach.OwnerUid, split, firstStomach);

            SoundSystem.Play(Filter.Pvs(targetUid), component.UseSound.GetSound(), targetUid, AudioParams.Default.WithVolume(-1f));
            _popupSystem.PopupEntity(Loc.GetString(component.EatMessage, ("food", component.Owner)), targetUid, Filter.Entities(targetUid));

            // Try to break all used utensils
            foreach (var utensil in usedUtensils)
            {
                _utensilSystem.TryBreak(utensil.OwnerUid, userUid);
            }

            if (component.UsesRemaining > 0)
            {
                return true;
            }

            if (string.IsNullOrEmpty(component.TrashPrototype))
            {
                component.Owner.QueueDelete();
                return true;
            }

            DeleteAndSpawnTrash(userUid, component);

            return true;
        }

        private void DeleteAndSpawnTrash(EntityUid userUid, FoodComponent component)
        {
            //We're empty. Become trash.
            var position = component.Owner.Transform.Coordinates;
            var finisher = component.Owner.EntityManager.SpawnEntity(component.TrashPrototype, position);

            // If the user is holding the item
            if (EntityManager.TryGetComponent(userUid, out HandsComponent? handsComponent) &&
                handsComponent.IsHolding(component.Owner))
            {
                component.Owner.Delete();

                // Put the trash in the user's hand
                if (finisher.TryGetComponent(out ItemComponent? item) &&
                    handsComponent.CanPutInHand(item))
                {
                    handsComponent.PutInHand(item);
                }
            }
            else
            {
                component.Owner.Delete();
            }
        }

        //No hands
        //TODO: DoAfter based on delay after food & drinks delay PR merged...
        private void AddEatVerb(EntityUid uid, FoodComponent component, GetInteractionVerbsEvent ev)
        {
            Logger.DebugS("action", "triggered");
            if (!ev.CanInteract ||
                !EntityManager.TryGetComponent(ev.User.Uid, out SharedBodyComponent? body) ||
                !_bodySystem.TryGetComponentsOnMechanisms<StomachComponent>(ev.User.Uid, out var stomachs, body))
                return;

            Verb verb = new();
            verb.Act = () =>
            {
                TryUseFood(uid, ev.User.Uid, ev.User.Uid, component);
            };
            
            verb.Text = Loc.GetString("food-system-verb-eat");
            verb.Priority = -1;
            ev.Verbs.Add(verb);
        }
    }
}
