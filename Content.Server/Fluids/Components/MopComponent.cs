using System.Threading.Tasks;
using Content.Server.DoAfter;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Popups;
using Content.Shared.Sound;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Fluids.Components
{
    /// <summary>
    /// For cleaning up puddles
    /// </summary>
    [RegisterComponent]
    public class MopComponent : Component, IAfterInteract
    {
        public override string Name => "Mop";
        public const string SolutionName = "mop";

        /// <summary>
        ///     Used to prevent do_after spam if we're currently mopping.
        /// </summary>
        public bool Mopping { get; private set; }

        public Solution? MopSolution
        {
            get
            {
                EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner, SolutionName, out var solution);
                return solution;
            }
        }

        public ReagentUnit MaxVolume
        {
            get => MopSolution?.MaxVolume ?? ReagentUnit.Zero;
            set
            {
                var solution = MopSolution;
                if (solution != null)
                {
                    solution.MaxVolume = value;
                }
            }
        }

        public ReagentUnit CurrentVolume => MopSolution?.CurrentVolume ?? ReagentUnit.Zero;

        // Currently there's a separate amount for pickup and dropoff so
        // Picking up a puddle requires multiple clicks
        // Dumping in a bucket requires 1 click
        // Long-term you'd probably use a cooldown and start the pickup once we have some form of global cooldown
        [DataField("pickup_amount")]
        public ReagentUnit PickupAmount { get; } = ReagentUnit.New(5);

        [DataField("pickup_sound")]
        private SoundSpecifier _pickupSound = new SoundPathSpecifier("/Audio/Effects/Fluids/slosh.ogg");

        /// <summary>
        ///     Multiplier for the do_after delay for how fast the mop works.
        /// </summary>
        [ViewVariables]
        [DataField("speed")] private float _mopSpeed = 1;

        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            /*
             * Functionality:
             * Essentially if we click on an empty tile spill our contents there
             * Otherwise, try to mop up the puddle (if it is a puddle).
             * It will try to destroy solution on the mop to do so, and if it is successful
             * will spill some of the mop's solution onto the puddle which will evaporate eventually.
             */

            if (!EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner, SolutionName, out var contents ) ||
                Mopping ||
                !eventArgs.InRangeUnobstructed(ignoreInsideBlocker: true, popup: true))
            {
                return false;
            }

            var currentVolume = CurrentVolume;

            if (eventArgs.Target == null)
            {
                if (currentVolume > 0)
                {
                    // Drop the liquid on the mop on to the ground
                    EntitySystem.Get<SolutionContainerSystem>().SplitSolution(Owner.Uid, contents, CurrentVolume)
                        .SpillAt(eventArgs.ClickLocation, "PuddleSmear");
                    return true;
                }

                return false;
            }

            if (!eventArgs.Target.TryGetComponent(out PuddleComponent? puddleComponent))
            {
                return false;
            }

            var puddleVolume = puddleComponent.CurrentVolume;

            if (currentVolume <= 0)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("mop-component-mop-is-dry-message"));
                return false;
            }

            Mopping = true;

            // So if the puddle has 20 units we mop in 2 seconds. Don't just store CurrentVolume given it can change so need to re-calc it anyway.
            var doAfterArgs = new DoAfterEventArgs(eventArgs.User, _mopSpeed * puddleVolume.Float() / 10.0f,
                target: eventArgs.Target)
            {
                BreakOnUserMove = true,
                BreakOnStun = true,
                BreakOnDamage = true,
            };
            var result = await EntitySystem.Get<DoAfterSystem>().WaitDoAfter(doAfterArgs);

            Mopping = false;

            if (result == DoAfterStatus.Cancelled ||
                Owner.Deleted ||
                puddleComponent.Deleted)
                return false;

            // Annihilate the puddle
            var transferAmount = ReagentUnit.Min(ReagentUnit.New(5), puddleComponent.CurrentVolume, CurrentVolume);
            var puddleCleaned = puddleComponent.CurrentVolume - transferAmount <= 0;

            if (transferAmount == 0)
            {
                if (
                    puddleComponent
                        .EmptyHolder) //The puddle doesn't actually *have* reagents, for example vomit because there's no "vomit" reagent.
                {
                    puddleComponent.Owner.Delete();
                    transferAmount = ReagentUnit.Min(ReagentUnit.New(5), CurrentVolume);
                    puddleCleaned = true;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                puddleComponent.SplitSolution(transferAmount);
            }

            if (
                puddleCleaned) //After cleaning the puddle, make a new puddle with solution from the mop as a "wet floor". Then evaporate it slowly.
            {
                EntitySystem.Get<SolutionContainerSystem>().SplitSolution(Owner.Uid, contents, transferAmount)
                    .SpillAt(eventArgs.ClickLocation, "PuddleSmear");
            }
            else
            {
                EntitySystem.Get<SolutionContainerSystem>().SplitSolution(Owner.Uid, contents, transferAmount);
            }

            SoundSystem.Play(Filter.Pvs(Owner), _pickupSound.GetSound(), Owner);

            return true;
        }
    }
}
