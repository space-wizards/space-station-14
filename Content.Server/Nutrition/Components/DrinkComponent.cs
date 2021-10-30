using Content.Server.Body.Behavior;
using Content.Server.Fluids.Components;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Sound;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;

namespace Content.Server.Nutrition.Components
{
    [RegisterComponent]
#pragma warning disable 618
    public class DrinkComponent : Component, IUse, IAfterInteract, IExamine
#pragma warning restore 618
    {
        [DataField("solution")]
        public string SolutionName { get; set; } = DefaultSolutionName;
        public const string DefaultSolutionName = "drink";

        public override string Name => "Drink";

        int IAfterInteract.Priority => 10;

        [ViewVariables]
        private bool _opened;

        [ViewVariables]
        [DataField("useSound")]
        private SoundSpecifier _useSound = new SoundPathSpecifier("/Audio/Items/drink.ogg");

        [ViewVariables]
        [DataField("isOpen")]
        internal bool DefaultToOpened;

        [ViewVariables(VVAccess.ReadWrite)]
        public ReagentUnit TransferAmount { get; [UsedImplicitly] private set; } = ReagentUnit.New(5);

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
                OnOpenedChanged();
            }
        }

        [ViewVariables] public bool Empty => IsEmpty();

        private bool IsEmpty()
        {
            var drainAvailable = EntitySystem.Get<SolutionContainerSystem>()
                .DrainAvailable(Owner);
            return drainAvailable <= 0;
        }

        [DataField("openSounds")]
        private SoundSpecifier _openSounds = new SoundCollectionSpecifier("canOpenSounds");

        [DataField("pressurized")] public bool Pressurized;

        [DataField("burstSound")] public SoundSpecifier BurstSound = new SoundPathSpecifier("/Audio/Effects/flash_bang.ogg");

        private void OnOpenedChanged()
        {
            var solutionSys = EntitySystem.Get<SolutionContainerSystem>();
            if (!solutionSys.TryGetSolution(Owner, SolutionName, out _))
            {
                return;
            }

            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(DrinkCanStateVisual.Opened, Opened);
            }

            if (Opened)
            {
                var refillable = Owner.EnsureComponent<RefillableSolutionComponent>();
                refillable.Solution = SolutionName;
                var drainable = Owner.EnsureComponent<DrainableSolutionComponent>();
                drainable.Solution = SolutionName;
            }
            else
            {
                Owner.RemoveComponent<RefillableSolutionComponent>();
                Owner.RemoveComponent<DrainableSolutionComponent>();
            }
        }

        bool IUse.UseEntity(UseEntityEventArgs args)
        {
            if (!Opened)
            {
                //Do the opening stuff like playing the sounds.
                SoundSystem.Play(Filter.Pvs(args.User), _openSounds.GetSound(), args.User, AudioParams.Default);

                Opened = true;
                return false;
            }

            if (!Owner.HasComponent<SolutionContainerManagerComponent>() ||
                EntitySystem.Get<SolutionContainerSystem>().DrainAvailable(Owner) <= 0)
            {
                args.User.PopupMessage(Loc.GetString("drink-component-on-use-is-empty", ("owner", Owner)));
                return true;
            }

            return TryUseDrink(args.User, args.User);
        }

        //Force feeding a drink to someone.
        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (eventArgs.Target == null)
            {
                return false;
            }

            return TryUseDrink(eventArgs.User, eventArgs.Target, true);
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (!Opened || !inDetailsRange)
            {
                return;
            }

            var color = Empty ? "gray" : "yellow";
            var openedText =
                Loc.GetString(Empty ? "drink-component-on-examine-is-empty" : "drink-component-on-examine-is-opened");
            message.AddMarkup(Loc.GetString("drink-component-on-examine-details-text", ("colorName", color), ("text", openedText)));
        }

        private bool TryUseDrink(IEntity user, IEntity target, bool forced = false)
        {
            if (!Opened)
            {
                target.PopupMessage(Loc.GetString("drink-component-try-use-drink-not-open", ("owner", Owner)));
                return false;
            }

            if (!EntitySystem.Get<SolutionContainerSystem>().TryGetDrainableSolution(Owner.Uid, out var interactions) ||
                interactions.DrainAvailable <= 0)
            {
                if (!forced)
                {
                    target.PopupMessage(Loc.GetString("drink-component-try-use-drink-is-empty", ("entity", Owner)));
                }

                return false;
            }

            if (!target.TryGetComponent(out SharedBodyComponent? body) ||
                !body.TryGetMechanismBehaviors<StomachBehavior>(out var stomachs))
            {
                target.PopupMessage(Loc.GetString("drink-component-try-use-drink-cannot-drink", ("owner", Owner)));
                return false;
            }


            if (user != target &&
                !user.InRangeUnobstructed(target, popup: true))
            {
                return false;
            }

            var solutionContainerSystem = EntitySystem.Get<SolutionContainerSystem>();
            var transferAmount = ReagentUnit.Min(TransferAmount, interactions.DrainAvailable);
            var drain = solutionContainerSystem.Drain(Owner.Uid, interactions, transferAmount);
            var firstStomach = stomachs.FirstOrDefault(stomach => stomach.CanTransferSolution(drain));

            // All stomach are full or can't handle whatever solution we have.
            if (firstStomach == null)
            {
                target.PopupMessage(Loc.GetString("drink-component-try-use-drink-had-enough", ("owner", Owner)));

                if (Owner.EntityManager.TryGetEntity(Owner.Uid, out var interactionEntity)
                    && !interactionEntity.HasComponent<RefillableSolutionComponent>())
                {
                    drain.SpillAt(target, "PuddleSmear");
                    return false;
                }

                solutionContainerSystem.Refill(Owner.Uid, interactions, drain);
                return false;
            }

            SoundSystem.Play(Filter.Pvs(target), _useSound.GetSound(), target, AudioParams.Default.WithVolume(-2f));

            target.PopupMessage(Loc.GetString("drink-component-try-use-drink-success-slurp"));

            // TODO: Account for partial transfer.

            drain.DoEntityReaction(target, ReactionMethod.Ingestion);

            firstStomach.TryTransferSolution(drain);

            return true;
        }
    }
}
