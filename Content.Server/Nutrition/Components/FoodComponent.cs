#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Body.Behavior;
using Content.Server.Chemistry.Components;
using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Notification;
using Content.Shared.Notification.Managers;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;

namespace Content.Server.Nutrition.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IAfterInteract))]
    public class FoodComponent : Component, IUse, IAfterInteract
    {
        public override string Name => "Food";

        [ViewVariables] [DataField("useSound")] protected virtual string? UseSound { get; set; } = "/Audio/Items/eatfood.ogg";

        [ViewVariables] [DataField("trash", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))] protected virtual string? TrashPrototype { get; set; }

        [ViewVariables] [DataField("transferAmount")] protected virtual ReagentUnit TransferAmount { get; set; } = ReagentUnit.New(5);

        [DataField("utensilsNeeded")] private UtensilType _utensilsNeeded = UtensilType.None;

        [ViewVariables]
        public int UsesRemaining
        {
            get
            {
                if (!Owner.TryGetComponent(out SolutionContainerComponent? solution))
                {
                    return 0;
                }

                return solution.CurrentVolume == 0
                    ? 0
                    : Math.Max(1, (int)Math.Ceiling((solution.CurrentVolume / TransferAmount).Float()));
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            Owner.EnsureComponentWarn<SolutionContainerComponent>();
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            if (_utensilsNeeded != UtensilType.None)
            {
                eventArgs.User.PopupMessage(Loc.GetString("food-you-need-utensil", ("utensil", _utensilsNeeded)));
                return false;
            }

            return TryUseFood(eventArgs.User, null);
        }

        // Feeding someone else
        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (eventArgs.Target == null)
            {
                return false;
            }

            TryUseFood(eventArgs.User, eventArgs.Target);
            return true;
        }

        public virtual bool TryUseFood(IEntity? user, IEntity? target, UtensilComponent? utensilUsed = null)
        {
            if (!Owner.TryGetComponent(out SolutionContainerComponent? solution))
            {
                return false;
            }

            if (user == null)
            {
                return false;
            }

            if (UsesRemaining <= 0)
            {
                user.PopupMessage(Loc.GetString("{0:TheName} is empty!", Owner));
                return false;
            }

            var trueTarget = target ?? user;

            if (!trueTarget.TryGetComponent(out IBody? body) ||
                !body.TryGetMechanismBehaviors<StomachBehavior>(out var stomachs))
            {
                return false;
            }

            var utensils = utensilUsed != null
                ? new List<UtensilComponent> {utensilUsed}
                : null;

            if (_utensilsNeeded != UtensilType.None)
            {
                utensils = new List<UtensilComponent>();
                var types = UtensilType.None;

                if (user.TryGetComponent(out HandsComponent? hands))
                {
                    foreach (var item in hands.GetAllHeldItems())
                    {
                        if (!item.Owner.TryGetComponent(out UtensilComponent? utensil))
                        {
                            continue;
                        }

                        utensils.Add(utensil);
                        types |= utensil.Types;
                    }
                }

                if (!types.HasFlag(_utensilsNeeded))
                {
                    trueTarget.PopupMessage(user, Loc.GetString("food-you-need-to-hold-utensil", ("utensil", _utensilsNeeded)));
                    return false;
                }
            }

            if (!user.InRangeUnobstructed(trueTarget, popup: true))
            {
                return false;
            }

            var transferAmount = ReagentUnit.Min(TransferAmount, solution.CurrentVolume);
            var split = solution.SplitSolution(transferAmount);
            var firstStomach = stomachs.FirstOrDefault(stomach => stomach.CanTransferSolution(split));

            if (firstStomach == null)
            {
                trueTarget.PopupMessage(user, Loc.GetString("You can't eat any more!"));
                return false;
            }

            // TODO: Account for partial transfer.

            split.DoEntityReaction(trueTarget, ReactionMethod.Ingestion);

            firstStomach.TryTransferSolution(split);

            if (UseSound != null)
            {
                SoundSystem.Play(Filter.Pvs(trueTarget), UseSound, trueTarget, AudioParams.Default.WithVolume(-1f));
            }

            trueTarget.PopupMessage(user, Loc.GetString("Nom"));

            // If utensils were used
            if (utensils != null)
            {
                foreach (var utensil in utensils)
                {
                    utensil.TryBreak(user);
                }
            }

            if (UsesRemaining > 0)
            {
                return true;
            }

            if (string.IsNullOrEmpty(TrashPrototype))
            {
                Owner.Delete();
                return true;
            }

            //We're empty. Become trash.
            var position = Owner.Transform.Coordinates;
            var finisher = Owner.EntityManager.SpawnEntity(TrashPrototype, position);

            // If the user is holding the item
            if (user.TryGetComponent(out HandsComponent? handsComponent) &&
                handsComponent.IsHolding(Owner))
            {
                Owner.Delete();

                // Put the trash in the user's hand
                if (finisher.TryGetComponent(out ItemComponent? item) &&
                    handsComponent.CanPutInHand(item))
                {
                    handsComponent.PutInHand(item);
                }
            }
            else
            {
                Owner.Delete();
            }

            return true;
        }
    }
}
