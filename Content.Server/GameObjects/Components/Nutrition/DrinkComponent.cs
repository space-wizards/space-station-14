using System;
using Content.Server.GameObjects.Components.Sound;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Nutrition;
using Content.Shared.Interfaces;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Nutrition
{
    [RegisterComponent]
    public class DrinkComponent : Component, IAfterAttack, IUse
    {
        public override string Name => "Drink";
        [ViewVariables]
        public Solution Contents => _contents;
        private Solution _contents;

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
            get
            {
                return _maxVolume;
            }
            set
            {
                _maxVolume = value;
            }
        }
        [ViewVariables]
        private int _maxVolume;

        private bool _despawnOnFinish;

        public int UsesLeft()
        {
            // In case transfer amount exceeds volume left
            if (Contents.TotalVolume == 0)
            {
                return 0;
            }
            return Math.Max(1, Contents.TotalVolume / _transferAmount);
        }


        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _contents, "contents", new Solution());
            serializer.DataField(ref _maxVolume, "max_volume", 4);
            serializer.DataField(ref _useSound, "use_sound", "/Audio/items/drink.ogg");
            // E.g. cola can when done or clear bottle, whatever
            // Currently this will enforce it has the same volume but this may change.
            serializer.DataField(ref _despawnOnFinish, "despawn_empty", true);
            serializer.DataField(ref _finishPrototype, "spawn_on_finish", null);
        }

        public override void Initialize()
        {
            base.Initialize();
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

        void UseDrink(IEntity user)
        {
            if (user == null)
            {
                return;
            }

            if (UsesLeft() == 0 && !_despawnOnFinish)
            {
                user.PopupMessage(user, "Empty");
                return;
            }

            if (user.TryGetComponent(out StomachComponent stomachComponent))
            {
                var transferAmount = Math.Min(_transferAmount, Contents.TotalVolume);
                var split = Contents.SplitSolution(transferAmount);
                if (stomachComponent.TryTransferSolution(split))
                {
                    if (_useSound != null)
                    {
                        Owner.GetComponent<SoundComponent>()?.Play(_useSound);
                        user.PopupMessage(user, "Slurp");
                    }
                }
                else
                {
                    // Add it back in
                    Contents.AddSolution(split);
                    user.PopupMessage(user, "Can't drink");
                }
            }

            // Drink containers are mostly transient.
            if (!_despawnOnFinish || UsesLeft() > 0)
            {
                return;

            }

            Owner.Delete();

            if (_finishPrototype != null)
            {
                var finisher = Owner.EntityManager.SpawnEntity(_finishPrototype);
                if (user.TryGetComponent(out HandsComponent handsComponent) && finisher.TryGetComponent(out ItemComponent itemComponent))
                {
                    if (handsComponent.CanPutInHand(itemComponent))
                    {
                        handsComponent.PutInHand(itemComponent);
                        return;
                    }
                }

                finisher.Transform.GridPosition = user.Transform.GridPosition;
                if (finisher.TryGetComponent(out DrinkComponent drinkComponent))
                {
                    drinkComponent.MaxVolume = MaxVolume;
                }
                return;
            }
        }
    }
}
