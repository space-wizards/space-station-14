#nullable enable
using Content.Server.GameObjects.Components.Chemistry;
using Content.Shared.Chemistry;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using System.Threading.Tasks;
using Content.Server.GameObjects.EntitySystems.DoAfter;
using Robust.Server.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Fluids
{
    /// <summary>
    /// For cleaning up puddles
    /// </summary>
    [RegisterComponent]
    public class MopComponent : Component, IAfterInteract
    {
        public override string Name => "Mop";

        public bool Mopping => _mopping;

        /// <summary>
        ///     Used to prevent do_after spam if we're currently mopping.
        /// </summary>
        private bool _mopping;

        public SolutionContainerComponent? Contents => Owner.GetComponentOrNull<SolutionContainerComponent>();

        public ReagentUnit MaxVolume
        {
            get => Owner.GetComponentOrNull<SolutionContainerComponent>()?.MaxVolume ?? ReagentUnit.Zero;
            set
            {
                if (Owner.TryGetComponent(out SolutionContainerComponent? solution))
                {
                    solution.MaxVolume = value;
                }
            }
        }

        public ReagentUnit CurrentVolume =>
            Owner.GetComponentOrNull<SolutionContainerComponent>()?.CurrentVolume ?? ReagentUnit.Zero;

        // Currently there's a separate amount for pickup and dropoff so
        // Picking up a puddle requires multiple clicks
        // Dumping in a bucket requires 1 click
        // Long-term you'd probably use a cooldown and start the pickup once we have some form of global cooldown
        public ReagentUnit PickupAmount => _pickupAmount;
        private ReagentUnit _pickupAmount;

        private string? _pickupSound;

        /// <summary>
        ///     Multiplier for the do_after delay for how fast the mop works.
        /// </summary>
        [ViewVariables]
        private float _mopSpeed;

        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataFieldCached(ref _pickupSound, "pickup_sound", "/Audio/Effects/Fluids/slosh.ogg");
            // The turbo mop will pickup more
            serializer.DataFieldCached(ref _pickupAmount, "pickup_amount", ReagentUnit.New(5));
            serializer.DataField(ref _mopSpeed, "speed", 1.0f);
        }

        public override void Initialize()
        {
            base.Initialize();

            Owner.EnsureComponentWarn(out SolutionContainerComponent _);
        }

        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            /*
             * Functionality:
             * Essentially if we click on an empty tile spill our contents there
             * Otherwise, try to mop up the puddle (if it is a puddle).
             * It will try to destroy solution on the mop to do so, and if it is successful
             * will spill some of the mop's solution onto the puddle which will evaporate eventually.
             */

            if (!Owner.TryGetComponent(out SolutionContainerComponent? contents) ||
                _mopping ||
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
                    contents.SplitSolution(CurrentVolume).SpillAt(eventArgs.ClickLocation, "PuddleSmear");
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
                Owner.PopupMessage(eventArgs.User, Loc.GetString("Mop needs to be wet!"));
                return false;
            }

            _mopping = true;

            // So if the puddle has 20 units we mop in 2 seconds. Don't just store CurrentVolume given it can change so need to re-calc it anyway.
            var doAfterArgs = new DoAfterEventArgs(eventArgs.User, _mopSpeed * puddleVolume.Float() / 10.0f, target: eventArgs.Target)
            {
                BreakOnUserMove = true,
                BreakOnStun = true,
                BreakOnDamage = true,
            };
            var result = await EntitySystem.Get<DoAfterSystem>().DoAfter(doAfterArgs);

            _mopping = false;

            if (result == DoAfterStatus.Cancelled ||
                Owner.Deleted ||
                puddleComponent.Deleted)
                return false;

            // Annihilate the puddle
            var transferAmount = ReagentUnit.Min(ReagentUnit.New(5), puddleComponent.CurrentVolume, CurrentVolume);
            var puddleCleaned = puddleComponent.CurrentVolume - transferAmount <= 0;

            if (transferAmount == 0)
            {
                if (puddleComponent.EmptyHolder) //The puddle doesn't actually *have* reagents, for example vomit because there's no "vomit" reagent.
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

            if (puddleCleaned) //After cleaning the puddle, make a new puddle with solution from the mop as a "wet floor". Then evaporate it slowly.
            {
                contents.SplitSolution(transferAmount).SpillAt(eventArgs.ClickLocation, "PuddleSmear");
            }
            else
            {
                contents.SplitSolution(transferAmount);
            }

            if (!string.IsNullOrWhiteSpace(_pickupSound))
            {
                EntitySystem.Get<AudioSystem>().PlayFromEntity(_pickupSound, Owner);
            }

            return true;
        }
    }
}
