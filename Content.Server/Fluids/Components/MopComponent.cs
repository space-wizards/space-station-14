using System.Threading.Tasks;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.DoAfter;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Popups;
using Content.Shared.Sound;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
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
        [Dependency] private readonly IEntityManager _entities = default!;

        public override string Name => "Mop";
        public const string SolutionName = "mop";

        /// <summary>
        ///     Used to prevent do_after spam if we're currently mopping.
        /// </summary>
        public bool Mopping { get; private set; }

        // MopSolution Object stores whatever solution the mop has absorbed.
        public Solution? MopSolution
        {
            get
            {
                EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner, SolutionName, out var solution);
                return solution;
            }
        }

        // MaxVolume is the Maximum volume the mop can absorb (however, this is defined in janitor.yml)
        public FixedPoint2 MaxVolume
        {
            get => MopSolution?.MaxVolume ?? FixedPoint2.Zero;
            set
            {
                var solution = MopSolution;
                if (solution != null)
                {
                    solution.MaxVolume = value;
                }
            }
        }

        // CurrentVolume is the volume the mop has absorbed.
        public FixedPoint2 CurrentVolume => MopSolution?.CurrentVolume ?? FixedPoint2.Zero;

        // AvailableVolume is the remaining volume capacity of the mop.
        public FixedPoint2 AvailableVolume => MopSolution?.AvailableVolume ?? FixedPoint2.Zero;

        // Currently there's a separate amount for pickup and dropoff so
        // Picking up a puddle requires multiple clicks
        // Dumping in a bucket requires 1 click
        // Long-term you'd probably use a cooldown and start the pickup once we have some form of global cooldown
        [DataField("pickup_amount")]
        public FixedPoint2 PickupAmount { get; } = FixedPoint2.New(10);

        /// <summary>
        ///     After cleaning a floor tile, leave this much reagent as a puddle. I.e., leave behind a wet floor.
        /// </summary>
        [DataField("residueAmount")]
        public FixedPoint2 ResidueAmount { get; } = FixedPoint2.New(5);

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
            var solutionSystem = EntitySystem.Get<SolutionContainerSystem>();
            var spillableSystem = EntitySystem.Get<SpillableSystem>();

            if (!solutionSystem.TryGetSolution(Owner, SolutionName, out var contents ) ||
                Mopping ||
                !eventArgs.InRangeUnobstructed(ignoreInsideBlocker: true, popup: true))
            {
                return false;
            }


            if (eventArgs.Target is not {Valid: true} target)
            {
                // Drop the liquid on the mop on to the ground
                var solution = solutionSystem.SplitSolution(Owner, contents, FixedPoint2.Min(ResidueAmount, CurrentVolume));
                spillableSystem.SpillAt(solution, eventArgs.ClickLocation, "PuddleSmear");
                return true;
            }

            if (!_entities.TryGetComponent(target, out PuddleComponent? puddleComponent) ||
                !solutionSystem.TryGetSolution((puddleComponent).Owner, puddleComponent.SolutionName, out var puddleSolution))
                return false;

            // if the mop is full
            if(AvailableVolume <= 0)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("mop-component-mop-is-full-message"));
                return false;
            }

            // Mopping duration (aka delay) should scale with PickupAmount and not puddle volume, because we are picking up a constant volume of solution with each click.
            var doAfterArgs = new DoAfterEventArgs(eventArgs.User, _mopSpeed * PickupAmount.Float() / 10.0f,
                target: target)
            {
                BreakOnUserMove = true,
                BreakOnStun = true,
                BreakOnDamage = true,
            };
            Mopping = true;
            var result = await EntitySystem.Get<DoAfterSystem>().WaitDoAfter(doAfterArgs);
            Mopping = false;

            if (result == DoAfterStatus.Cancelled ||
                _entities.Deleted(Owner) ||
                puddleComponent.Deleted)
                return false;


            FixedPoint2 transferAmount;
            // does the puddle actually have reagents? it might not if its a weird cosmetic entity.
            if (puddleSolution.TotalVolume == 0)
                transferAmount = FixedPoint2.Min(PickupAmount, AvailableVolume);
            else
                transferAmount = FixedPoint2.Min(PickupAmount, puddleSolution.TotalVolume, AvailableVolume);


            // is the puddle cleaned?
            bool isCleaned = (puddleSolution.TotalVolume - transferAmount <= 0);

            // Transfers solution from the puddle to the mop
            solutionSystem.TryAddSolution(Owner, contents, solutionSystem.SplitSolution(target, puddleSolution, transferAmount));

            if (isCleaned)
            {
                // deletes the puddle
                _entities.DeleteEntity(puddleComponent.Owner);
            }
            SoundSystem.Play(Filter.Pvs(Owner), _pickupSound.GetSound(), Owner);

            return true;
        }
    }
}
