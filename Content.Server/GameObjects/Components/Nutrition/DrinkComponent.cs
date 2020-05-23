using System;
using System.Linq;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.GameObjects.Components.Sound;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Utility;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Nutrition;
using Content.Shared.Interfaces;
using Content.Shared.Maths;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.Components.Nutrition
{
    [RegisterComponent]
    public class DrinkComponent : Component, IAfterInteract, IUse
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

        public ReagentUnit TransferAmount => _transferAmount;
        [ViewVariables]
        private ReagentUnit _transferAmount = ReagentUnit.New(2);

        public ReagentUnit MaxVolume
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
            return Math.Max(1, (int)Math.Ceiling((_contents.CurrentVolume / _transferAmount).Float()));
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
            _appearanceComponent?.SetData(SharedFoodComponent.FoodVisuals.MaxUses, MaxVolume.Float());
            _updateAppearance();
        }

        private void _updateAppearance()
        {
            _appearanceComponent?.SetData(SharedFoodComponent.FoodVisuals.Visual, _contents.CurrentVolume.Float());
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            UseDrink(eventArgs.User);

            return true;
        }

        void IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (!InteractionChecks.InRangeUnobstructed(eventArgs)) return;

            UseDrink(eventArgs.Target);
        }

        private void UseDrink(IEntity targetEntity)
        {
            if (targetEntity == null)
            {
                return;
            }

            if (UsesLeft() == 0 && !_despawnOnFinish)
            {
                targetEntity.PopupMessage(targetEntity, _localizationManager.GetString("Empty"));
                return;
            }

            if (targetEntity.TryGetComponent(out StomachComponent stomachComponent))
            {
                _drinking = true;
                var transferAmount = ReagentUnit.Min(_transferAmount, _contents.CurrentVolume);
                var split = _contents.SplitSolution(transferAmount);
                if (stomachComponent.TryTransferSolution(split))
                {
                    // When we split Finish gets called which may delete the can so need to use the entity system for sound
                    if (_useSound != null)
                    {
                        var audioSystem = EntitySystem.Get<AudioSystem>();
                        audioSystem.Play(_useSound, Owner, AudioParams.Default.WithVolume(-2f));
                        targetEntity.PopupMessage(targetEntity, _localizationManager.GetString("Slurp"));
                    }
                }
                else
                {
                    // Add it back in
                    _contents.TryAddSolution(split);
                    targetEntity.PopupMessage(targetEntity, _localizationManager.GetString("Can't drink"));
                }
                _drinking = false;
            }
        }
    }
}
