using System;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.GameObjects.Components.Sound;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Nutrition;
using Content.Shared.Interfaces;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Nutrition
{
    [RegisterComponent]
    public class DrinkComponent : Component, IAfterAttack, IUse, ISolutionChange
    {
#pragma warning disable 649
        [Dependency] private readonly ILocalizationManager _localizationManager;
#pragma warning restore 649
        public override string Name => "Drink";
        [ViewVariables]
        private SolutionComponent _contents;

        private AppearanceComponent _appearanceComponent;

        [ViewVariables]
        private string _useSound;
        [ViewVariables]
        private string _finishPrototype;

        public int TransferAmount => _transferAmount;
        [ViewVariables]
        private int _transferAmount = 2;

        public int MaxVolume
        {
            get => _contents.MaxVolume;
            set => _contents.MaxVolume = value;
        }

        private bool _despawnOnFinish;

        private bool _drinking;

        public int UsesLeft()
        {
            // In case transfer amount exceeds volume left
            if (_contents.CurrentVolume == 0)
            {
                return 0;
            }
            return Math.Max(1, _contents.CurrentVolume / _transferAmount);
        }


        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _useSound, "use_sound", "/Audio/items/drink.ogg");
            // E.g. cola can when done or clear bottle, whatever
            // Currently this will enforce it has the same volume but this may change. - TODO: this should be implemented in a separate component
            serializer.DataField(ref _despawnOnFinish, "despawn_empty", false);
            serializer.DataField(ref _finishPrototype, "spawn_on_finish", null);
        }

        protected override void Startup()
        {
            base.Startup();
            _contents = Owner.GetComponent<SolutionComponent>();
            _contents.Capabilities = SolutionCaps.PourIn
                                     | SolutionCaps.PourOut
                                     | SolutionCaps.Injectable;
            _drinking = false;
            Owner.TryGetComponent(out AppearanceComponent appearance);
            _appearanceComponent = appearance;
            _appearanceComponent?.SetData(SharedFoodComponent.FoodVisuals.MaxUses, MaxVolume);
            _updateAppearance();
        }

        private void _updateAppearance()
        {
            _appearanceComponent?.SetData(SharedFoodComponent.FoodVisuals.Visual, UsesLeft());
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            UseDrink(eventArgs.User);

            return true;
        }

        void IAfterAttack.AfterAttack(AfterAttackEventArgs eventArgs)
        {
            UseDrink(eventArgs.Attacked);
        }

        private void UseDrink(IEntity user)
        {
            if (user == null)
            {
                return;
            }

            if (UsesLeft() == 0 && !_despawnOnFinish)
            {
                user.PopupMessage(user, _localizationManager.GetString("Empty"));
                return;
            }

            if (user.TryGetComponent(out StomachComponent stomachComponent))
            {
                _drinking = true;
                var transferAmount = Math.Min(_transferAmount, _contents.CurrentVolume);
                var split = _contents.SplitSolution(transferAmount);
                if (stomachComponent.TryTransferSolution(split))
                {
                    if (_useSound != null)
                    {
                        Owner.GetComponent<SoundComponent>()?.Play(_useSound);
                        user.PopupMessage(user, _localizationManager.GetString("Slurp"));
                    }
                }
                else
                {
                    // Add it back in
                    _contents.TryAddSolution(split);
                    user.PopupMessage(user, _localizationManager.GetString("Can't drink"));
                }
                _drinking = false;
            }

            //Finish(user);
        }

        /// <summary>
        /// Trigger finish behavior in the drink if applicable.
        /// Depending on the drink this will either delete it,
        /// or convert it to another entity, like an empty variant.
        /// </summary>
        /// <param name="user">The entity that is using the drink</param>
        /*
         public void Finish(IEntity user)
        {
            // Drink containers are mostly transient.
            // are you sure about that
            if (_drinking || !_despawnOnFinish || UsesLeft() > 0)
                return;

            var gridPos = Owner.Transform.GridPosition;
            Owner.Delete();

            if (_finishPrototype == null || user == null)
                return;

            var finisher = Owner.EntityManager.SpawnEntity(_finishPrototype, Owner.Transform.GridPosition);
            if (user.TryGetComponent(out HandsComponent handsComponent) && finisher.TryGetComponent(out ItemComponent itemComponent))
            {
                if (handsComponent.CanPutInHand(itemComponent))
                {
                    handsComponent.PutInHand(itemComponent);
                    return;
                }
            }

            finisher.Transform.GridPosition = gridPos;
            if (finisher.TryGetComponent(out DrinkComponent drinkComponent))
            {
                drinkComponent.MaxVolume = MaxVolume;
            }
        }*/

        void ISolutionChange.SolutionChanged(SolutionChangeEventArgs eventArgs) { } //Finish(null);
    }
}
