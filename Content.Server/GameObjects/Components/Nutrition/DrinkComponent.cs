#nullable enable
using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Body.Behavior;
using Content.Server.GameObjects.Components.Fluids;
using Content.Shared.Audio;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Chemistry;
using Content.Shared.GameObjects.Components.Nutrition;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
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
    public class DrinkComponent : Component, IUse, IAfterInteract, ISolutionChange, IExamine, ILand
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        public override string Name => "Drink";

        [ViewVariables]
        private bool _opened;

        [ViewVariables]
        private string _useSound = string.Empty;

        [ViewVariables]
        private bool _defaultToOpened;

        [ViewVariables(VVAccess.ReadWrite)]
        public ReagentUnit TransferAmount { get; [UsedImplicitly] private set; } = ReagentUnit.New(2);

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Opened
        {
            get => _opened;
            set
            {
                if (_opened == value)
                {
                    return;
                }

                _opened = value;
                OpenedChanged();
            }
        }

        [ViewVariables]
        public bool Empty => Owner.GetComponentOrNull<ISolutionInteractionsComponent>()?.DrainAvailable <= 0;

        private string _soundCollection = string.Empty;
        private bool _pressurized;
        private string _burstSound = string.Empty;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _useSound, "useSound", "/Audio/Items/drink.ogg");
            serializer.DataField(ref _defaultToOpened, "isOpen", false); // For things like cups of coffee.
            serializer.DataField(ref _soundCollection, "openSounds", "canOpenSounds");
            serializer.DataField(ref _pressurized, "pressurized", false);
            serializer.DataField(ref _burstSound, "burstSound", "/Audio/Effects/flash_bang.ogg");
        }

        public override void Initialize()
        {
            base.Initialize();

            Opened = _defaultToOpened;
            UpdateAppearance();
        }

        private void OpenedChanged()
        {
            if (!Owner.TryGetComponent(out SharedSolutionContainerComponent? contents))
            {
                return;
            }

            if (Opened)
            {
                contents.Capabilities |= SolutionContainerCaps.Refillable | SolutionContainerCaps.Drainable;
            }
            else
            {
                contents.Capabilities &= ~(SolutionContainerCaps.Refillable | SolutionContainerCaps.Drainable);
            }
        }

        void ISolutionChange.SolutionChanged(SolutionChangeEventArgs eventArgs)
        {
            UpdateAppearance();
        }

        private void UpdateAppearance()
        {
            if (!Owner.TryGetComponent(out AppearanceComponent? appearance) ||
                !Owner.TryGetComponent(out ISolutionInteractionsComponent? contents))
            {
                return;
            }

            appearance.SetData(SharedFoodComponent.FoodVisuals.Visual, contents.DrainAvailable.Float());
        }

        bool IUse.UseEntity(UseEntityEventArgs args)
        {
            if (!Opened)
            {
                //Do the opening stuff like playing the sounds.
                var soundCollection = _prototypeManager.Index<SoundCollectionPrototype>(_soundCollection);
                var file = _random.Pick(soundCollection.PickFiles);

                EntitySystem.Get<AudioSystem>().PlayFromEntity(file, args.User, AudioParams.Default);
                Opened = true;
                return false;
            }

            if (!Owner.TryGetComponent(out ISolutionInteractionsComponent? contents) ||
                contents.DrainAvailable <= 0)
            {
                args.User.PopupMessage(Loc.GetString("{0:theName} is empty!", Owner));
                return true;
            }

            return TryUseDrink(args.User);
        }

        //Force feeding a drink to someone.
        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (eventArgs.Target == null)
            {
                return false;
            }

            TryUseDrink(eventArgs.Target, true);

            return true;
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

        private bool TryUseDrink(IEntity target, bool forced = false)
        {
            if (!Opened)
            {
                target.PopupMessage(Loc.GetString("Open {0:theName} first!", Owner));
                return false;
            }

            if (!Owner.TryGetComponent(out ISolutionInteractionsComponent? interactions) ||
                !interactions.CanDrain ||
                interactions.DrainAvailable <= 0)
            {
                if (!forced)
                {
                    target.PopupMessage(Loc.GetString("{0:theName} is empty!", Owner));
                }

                return false;
            }

            if (!target.TryGetComponent(out IBody? body) ||
                !body.TryGetMechanismBehaviors<StomachBehavior>(out var stomachs))
            {
                target.PopupMessage(Loc.GetString("You can't drink {0:theName}!", Owner));
                return false;
            }

            var transferAmount = ReagentUnit.Min(TransferAmount, interactions.DrainAvailable);
            var drain = interactions.Drain(transferAmount);
            var firstStomach = stomachs.FirstOrDefault(stomach => stomach.CanTransferSolution(drain));

            // All stomach are full or can't handle whatever solution we have.
            if (firstStomach == null)
            {
                target.PopupMessage(Loc.GetString("You've had enough {0:theName}!", Owner));

                if (!interactions.CanRefill)
                {
                    drain.SpillAt(target, "PuddleSmear");
                    return false;
                }

                interactions.Refill(drain);
                return false;
            }

            if (!string.IsNullOrEmpty(_useSound))
            {
                EntitySystem.Get<AudioSystem>().PlayFromEntity(_useSound, target, AudioParams.Default.WithVolume(-2f));
            }

            target.PopupMessage(Loc.GetString("Slurp"));
            UpdateAppearance();

            // TODO: Account for partial transfer.

            drain.DoEntityReaction(target, ReactionMethod.Ingestion);

            firstStomach.TryTransferSolution(drain);

            return true;
        }

        void ILand.Land(LandEventArgs eventArgs)
        {
            if (_pressurized &&
                !Opened &&
                _random.Prob(0.25f) &&
                Owner.TryGetComponent(out ISolutionInteractionsComponent? interactions))
            {
                Opened = true;

                if (!interactions.CanDrain)
                {
                    return;
                }

                var solution = interactions.Drain(interactions.DrainAvailable);
                solution.SpillAt(Owner, "PuddleSmear");

                EntitySystem.Get<AudioSystem>().PlayFromEntity(_burstSound, Owner,
                    AudioParams.Default.WithVolume(-4));
            }
        }
    }
}
