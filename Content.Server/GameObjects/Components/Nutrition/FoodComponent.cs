using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.GameObjects.Components.Utensil;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Utility;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Utensil;
using Content.Shared.Interfaces;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Nutrition
{
    [RegisterComponent]
    [ComponentReference(typeof(IAfterInteract))]
    public class FoodComponent : Component, IUse, IAfterInteract
    {
#pragma warning disable 649
        [Dependency] private readonly IEntitySystemManager _entitySystem;
#pragma warning restore 649
        public override string Name => "Food";

        [ViewVariables]
        private string _useSound;
        [ViewVariables]
        private string _trashPrototype;
        [ViewVariables]
        private SolutionComponent _contents;
        [ViewVariables]
        private ReagentUnit _transferAmount;
        private UtensilKind _utensilsNeeded;

        public int UsesRemaining => _contents.CurrentVolume == 0
            ?
            0 : Math.Max(1, (int)Math.Ceiling((_contents.CurrentVolume / _transferAmount).Float()));

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _useSound, "useSound", "/Audio/items/eatfood.ogg");
            serializer.DataField(ref _transferAmount, "transferAmount", ReagentUnit.New(5));
            serializer.DataField(ref _trashPrototype, "trash", "TrashPlate");

            if (serializer.Reading)
            {
                var utensils = serializer.ReadDataField("utensils", new List<UtensilKind>());
                foreach (var utensil in utensils)
                {
                    _utensilsNeeded |= utensil;
                    Dirty();
                }
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            _contents = Owner.GetComponent<SolutionComponent>();

        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            return TryUseFood(eventArgs.User, null);
        }

        void IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            TryUseFood(eventArgs.User, eventArgs.Target);
        }

        internal bool TryUseFood(IEntity user, IEntity target, UtensilKind utensil = UtensilKind.None)
        {
            if (user == null)
            {
                return false;
            }

            if (UsesRemaining <= 0)
            {
                user.PopupMessage(user, Loc.GetString($"The {Owner.Name} is empty!"));
                return false;
            }

            var trueTarget = target ?? user;

            if (!utensil.HasFlag(_utensilsNeeded))
            {
                trueTarget.PopupMessage(user, Loc.GetString("You need to be holding a {0} to eat that!", _utensilsNeeded));
                return false;
            }

            if (!trueTarget.TryGetComponent(out StomachComponent stomach))
            {
                return false;
            }

            var transferAmount = ReagentUnit.Min(_transferAmount, _contents.CurrentVolume);
            var split = _contents.SplitSolution(transferAmount);
            if (!stomach.TryTransferSolution(split))
            {
                _contents.TryAddSolution(split);
                trueTarget.PopupMessage(user, Loc.GetString("You can't eat any more!"));
                return false;
            }

            _entitySystem.GetEntitySystem<AudioSystem>()
                .PlayFromEntity(_useSound, trueTarget, AudioParams.Default.WithVolume(-1f));
            trueTarget.PopupMessage(user, Loc.GetString("Nom"));

            if (UsesRemaining > 0)
            {
                return true;
            }

            //We're empty. Become trash.
            var position = Owner.Transform.GridPosition;
            Owner.Delete();
            var finisher = Owner.EntityManager.SpawnEntity(_trashPrototype, position);
            if (user.TryGetComponent(out HandsComponent handsComponent) && finisher.TryGetComponent(out ItemComponent itemComponent))
            {
                if (handsComponent.CanPutInHand(itemComponent))
                {
                    handsComponent.PutInHand(itemComponent);
                    return true;
                }
            }
            finisher.Transform.GridPosition = user.Transform.GridPosition;
            return true;
        }
    }
}
