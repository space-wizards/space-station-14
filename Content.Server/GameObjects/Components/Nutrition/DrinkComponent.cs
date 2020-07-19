using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.GameObjects.EntitySystems.Click;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Content.Shared.Audio;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Nutrition;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Nutrition
{
    [RegisterComponent]
    [ComponentReference(typeof(IAfterInteract))]
    public class DrinkComponent : Component, IUse, IAfterInteract, ISolutionChange,IExamine
    {
#pragma warning disable 649
        [Dependency] private readonly IPrototypeManager _prototypeManager;
        [Dependency] private readonly IRobustRandom _random;
#pragma warning restore 649
        public override string Name => "Drink";

        [ViewVariables]
        private SolutionComponent _contents;
        [ViewVariables]
        private string _useSound;
        [ViewVariables]
        private bool _defaultToOpened;
        [ViewVariables]
        public ReagentUnit TransferAmount { get; private set; } = ReagentUnit.New(2);

        [ViewVariables]
        public bool Opened => _opened;

        [ViewVariables]
        public bool Empty => _contents.CurrentVolume.Float() <= 0;

        private AppearanceComponent _appearanceComponent;
        private bool _opened = false;
        private string _soundCollection;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _useSound, "useSound", "/Audio/Items/drink.ogg");
            serializer.DataField(ref _defaultToOpened, "isOpen", false); //For things like cups of coffee.
            serializer.DataField(ref _soundCollection, "openSounds","canOpenSounds");
        }

        public override void Initialize()
        {
            base.Initialize();
            Owner.TryGetComponent(out _appearanceComponent);
            if(!Owner.TryGetComponent(out _contents))
            {
                _contents = Owner.AddComponent<SolutionComponent>();
            }

            _contents.Capabilities = SolutionCaps.PourIn
                                     | SolutionCaps.PourOut
                                     | SolutionCaps.Injectable;
            _opened = _defaultToOpened;
            UpdateAppearance();
        }

        void ISolutionChange.SolutionChanged(SolutionChangeEventArgs eventArgs)
        {
            UpdateAppearance();
        }


        private void UpdateAppearance()
        {
            _appearanceComponent?.SetData(SharedFoodComponent.FoodVisuals.Visual, _contents.CurrentVolume.Float());
        }
        bool IUse.UseEntity(UseEntityEventArgs args)
        {
            if (!_opened)
            {
                //Do the opening stuff like playing the sounds.
                var soundCollection = _prototypeManager.Index<SoundCollectionPrototype>(_soundCollection);
                var file = _random.Pick(soundCollection.PickFiles);

                EntitySystem.Get<AudioSystem>().PlayFromEntity(file, args.User, AudioParams.Default);
                _opened = true;
                return false;
            }
            return TryUseDrink(args.User);
        }

        //Force feeding a drink to someone.
        void IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            TryUseDrink(eventArgs.Target);
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (!Opened || !inDetailsRange)
            {
                return;
            }
            var color = Empty ? "gray" : "yellow";
            var openedText = Loc.GetString(Empty ? "Empty" : "Opened");
            message.AddMarkup(Loc.GetString("[color={0}]{1}[/color]", color, openedText));

        }

        private bool TryUseDrink(IEntity target)
        {
            if (target == null)
            {
                return false;
            }

            if (!_opened)
            {
                target.PopupMessage(target, Loc.GetString("Open it first!"));
                return false;
            }

            if (_contents.CurrentVolume.Float() <= 0)
            {
                target.PopupMessage(target, Loc.GetString("It's empty!"));
                return false;
            }

            if (!target.TryGetComponent(out StomachComponent stomachComponent))
            {
                return false;
            }

            var transferAmount = ReagentUnit.Min(TransferAmount, _contents.CurrentVolume);
            var split = _contents.SplitSolution(transferAmount);
            if (stomachComponent.TryTransferSolution(split))
            {
                if (_useSound == null) return false;
                EntitySystem.Get<AudioSystem>().PlayFromEntity(_useSound, target, AudioParams.Default.WithVolume(-2f));
                target.PopupMessage(target, Loc.GetString("Slurp"));
                UpdateAppearance();
                return true;
            }

            //Stomach was full or can't handle whatever solution we have.
            _contents.TryAddSolution(split);
            target.PopupMessage(target, Loc.GetString("You've had enough {0}!", Owner.Name));
            return false;
        }
    }
}
