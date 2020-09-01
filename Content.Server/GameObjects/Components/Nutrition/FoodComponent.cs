#nullable enable
using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Body.Digestive;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Utensil;
using Content.Server.Utility;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Utensil;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Nutrition
{
    [RegisterComponent]
    [ComponentReference(typeof(IAfterInteract))]
    public class FoodComponent : Component, IUse, IAfterInteract
    {
        [Dependency] private readonly IEntitySystemManager _entitySystem = default!;

        public override string Name => "Food";

        [ViewVariables] private string _useSound = "";
        [ViewVariables] private string? _trashPrototype;
        [ViewVariables] private ReagentUnit _transferAmount;
        private UtensilType _utensilsNeeded;

        [ViewVariables]
        public int UsesRemaining
        {
            get
            {
                if (!Owner.TryGetComponent(out SolutionComponent? solution))
                {
                    return 0;
                }

                return solution.CurrentVolume == 0
                    ? 0
                    : Math.Max(1, (int)Math.Ceiling((solution.CurrentVolume / _transferAmount).Float()));
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _useSound, "useSound", "/Audio/Items/eatfood.ogg");
            serializer.DataField(ref _transferAmount, "transferAmount", ReagentUnit.New(5));
            serializer.DataField(ref _trashPrototype, "trash", null);

            serializer.DataReadWriteFunction(
                "utensils",
                new List<UtensilType>(),
                types => types.ForEach(type => _utensilsNeeded |= type),
                () =>
                {
                    var types = new List<UtensilType>();

                    foreach (var type in (UtensilType[]) Enum.GetValues(typeof(UtensilType)))
                    {
                        if ((_utensilsNeeded & type) != 0)
                        {
                            types.Add(type);
                        }
                    }

                    return types;
                });
        }

        public override void Initialize()
        {
            base.Initialize();
            Owner.EnsureComponent<SolutionComponent>();
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            if (_utensilsNeeded != UtensilType.None)
            {
                eventArgs.User.PopupMessage(Loc.GetString("You need to use a {0} to eat that!", _utensilsNeeded));
                return false;
            }

            return TryUseFood(eventArgs.User, null);
        }

        // Feeding someone else
        void IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (eventArgs.Target == null)
            {
                return;
            }

            TryUseFood(eventArgs.User, eventArgs.Target);
        }

        public virtual bool TryUseFood(IEntity? user, IEntity? target, UtensilComponent? utensilUsed = null)
        {
            if (!Owner.TryGetComponent(out SolutionComponent? solution))
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

            if (!trueTarget.TryGetComponent(out StomachComponent? stomach))
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
                    trueTarget.PopupMessage(user, Loc.GetString("You need to be holding a {0} to eat that!", _utensilsNeeded));
                    return false;
                }
            }

            if (!user.InRangeUnobstructed(trueTarget, popup: true))
            {
                return false;
            }

            var transferAmount = ReagentUnit.Min(_transferAmount, solution.CurrentVolume);
            var split = solution.SplitSolution(transferAmount);
            if (!stomach.TryTransferSolution(split))
            {
                solution.TryAddSolution(split);
                trueTarget.PopupMessage(user, Loc.GetString("You can't eat any more!"));
                return false;
            }

            _entitySystem.GetEntitySystem<AudioSystem>()
                .PlayFromEntity(_useSound, trueTarget, AudioParams.Default.WithVolume(-1f));
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

            if (string.IsNullOrEmpty(_trashPrototype))
            {
                Owner.Delete();
                return true;
            }

            //We're empty. Become trash.
            var position = Owner.Transform.GridPosition;
            var finisher = Owner.EntityManager.SpawnEntity(_trashPrototype, position);

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
