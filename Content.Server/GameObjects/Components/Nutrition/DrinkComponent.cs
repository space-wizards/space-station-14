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
    public class DrinkComponent : Component, IAfterAttack, IUse
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

        private Solution _initialContents; // This is just for loading from yaml

        private bool _despawnOnFinish;

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
            serializer.DataField(ref _initialContents, "contents", null);
            serializer.DataField(ref _useSound, "use_sound", "/Audio/items/drink.ogg");
            // E.g. cola can when done or clear bottle, whatever
            // Currently this will enforce it has the same volume but this may change.
            serializer.DataField(ref _despawnOnFinish, "despawn_empty", true);
            serializer.DataField(ref _finishPrototype, "spawn_on_finish", null);
        }

        public override void Initialize()
        {
            base.Initialize();
            if (_contents == null)
            {
                if (Owner.TryGetComponent(out SolutionComponent solutionComponent))
                {
                    _contents = solutionComponent;
                }
                else
                {
                    _contents = Owner.AddComponent<SolutionComponent>();
                    _contents.Initialize();
                }
            }

            _contents.MaxVolume = _initialContents.TotalVolume;
        }

        protected override void Startup()
        {
            base.Startup();
            if (_initialContents != null)
            {
                _contents.TryAddSolution(_initialContents, true, true);
            }
            _initialContents = null;
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
                user.PopupMessage(user, _localizationManager.GetString("Empty"));
                return;
            }

            if (user.TryGetComponent(out StomachComponent stomachComponent))
            {
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
            }

            // Drink containers are mostly transient.
            if (!_despawnOnFinish || UsesLeft() > 0)
            {
                return;

            }

            Owner.Delete();

            if (_finishPrototype != null)
            {
                var finisher = Owner.EntityManager.SpawnEntity(_finishPrototype, Owner.Transform.GridPosition);
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
