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
    public class FoodComponent : Component, IAfterAttack, IUse
    {
#pragma warning disable 649
        [Dependency] private readonly ILocalizationManager _localizationManager;
#pragma warning restore 649
        // Currently the design is similar to drinkcomponent but it's susceptible to change so left as is for now.
        public override string Name => "Food";

        private AppearanceComponent _appearanceComponent;

        [ViewVariables]
        private string _useSound;
        [ViewVariables]
        private string _finishPrototype;
        [ViewVariables]
        private SolutionComponent _contents;
        [ViewVariables]
        private int _transferAmount;

        private Solution _initialContents; // This is just for loading from yaml

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            // Default is 1 use restoring 30
            serializer.DataField(ref _initialContents, "contents", null);
            serializer.DataField(ref _useSound, "use_sound", "/Audio/items/eatfood.ogg");
            // Default is transfer 30 units
            serializer.DataField(ref _transferAmount, "transfer_amount", 5);
            // E.g. empty chip packet when done
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
            if (_contents.CurrentVolume == 0)
            {
                _contents.TryAddReagent("chem.Nutriment", 5, out _);
            }
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
            if (_contents.CurrentVolume == 0)
            {
                return 0;
            }
            return Math.Max(1, _contents.CurrentVolume / _transferAmount);
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
                user.PopupMessage(user, _localizationManager.GetString("Empty"));
            }
            else
            {
                // TODO: Add putting food back in boxes here?
                if (user.TryGetComponent(out StomachComponent stomachComponent))
                {
                    var transferAmount = Math.Min(_transferAmount, _contents.CurrentVolume);
                    var split = _contents.SplitSolution(transferAmount);
                    if (stomachComponent.TryTransferSolution(split))
                    {
                        if (_useSound != null)
                        {
                            Owner.GetComponent<SoundComponent>()?.Play(_useSound);
                            user.PopupMessage(user, _localizationManager.GetString("Nom"));
                        }
                    }
                    else
                    {
                        // Add it back in
                        _contents.TryAddSolution(split);
                        user.PopupMessage(user, _localizationManager.GetString("Can't eat"));
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
                return;
            }
        }
    }
}
