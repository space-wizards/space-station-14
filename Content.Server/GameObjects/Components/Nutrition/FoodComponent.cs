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

namespace Content.Server.GameObjects.Components.Nutrition
{
    [RegisterComponent]
    public class FoodComponent : Component, IAfterAttack, IUse
    {
        // Currently the design is similar to drinkcomponent but it's susceptible to change so left as is for now.
        public override string Name => "Food";

        private AppearanceComponent _appearanceComponent;

        private string _useSound;
        private string _finishPrototype;
        public Solution Contents => _contents;
        private Solution _contents;
        private int _transferAmount;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            // Default is 1 use restoring 30
            serializer.DataField(ref _contents, "contents",
                new Solution("chem.Nutriment",
                    30 / StomachComponent.NutrimentFactor));
            serializer.DataField(ref _useSound, "use_sound", "/Audio/items/eatfood.ogg");
            // Default is transfer 30 units
            serializer.DataField(ref _transferAmount,
                "transfer_amount",
                30 / StomachComponent.NutrimentFactor);
            // E.g. empty chip packet when done
            serializer.DataField(ref _finishPrototype, "spawn_on_finish", null);
        }

        public override void Initialize()
        {
            base.Initialize();
            Owner.TryGetComponent(out AppearanceComponent appearance);
            _appearanceComponent = appearance;
            // UsesLeft() at the start should match the max, at least currently.
            _appearanceComponent?.SetData(SharedFoodComponent.FoodVisuals.MaxUses, UsesLeft());
            _updateAppearance();
        }

        private void _updateAppearance()
        {
            _appearanceComponent?.SetData(SharedFoodComponent.FoodVisuals.Visual, UsesLeft());
        }

        public int UsesLeft()
        {
            // In case transfer amount exceeds volume left
            if (Contents.TotalVolume == 0)
            {
                return 0;
            }
            return Math.Max(1, Contents.TotalVolume / _transferAmount);
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            UseFood(eventArgs.User);

            return true;
        }

        void IAfterAttack.AfterAttack(AfterAttackEventArgs eventArgs)
        {
            UseFood(eventArgs.Attacked);
        }

        void UseFood(IEntity user)
        {
            if (user == null)
            {
                return;
            }

            if (UsesLeft() == 0)
            {
                user.PopupMessage(user, "Empty");
            }
            else
            {
                // TODO: Add putting food back in boxes here?
                if (user.TryGetComponent(out StomachComponent stomachComponent))
                {
                    var transferAmount = Math.Min(_transferAmount, Contents.TotalVolume);
                    var split = Contents.SplitSolution(transferAmount);
                    if (stomachComponent.TryTransferSolution(split))
                    {
                        if (_useSound != null)
                        {
                            Owner.GetComponent<SoundComponent>()?.Play(_useSound);
                            user.PopupMessage(user, "Nom");
                        }
                    }
                    else
                    {
                        // Add it back in
                        Contents.AddSolution(split);
                        user.PopupMessage(user, "Can't eat");
                    }
                }
            }

            if (UsesLeft() > 0)
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
                return;
            }
        }
    }
}
