#nullable enable
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.Utility;
using Content.Shared.Chemistry;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Fluids
{
    /// <summary>
    /// For cleaning up puddles
    /// </summary>
    [RegisterComponent]
    public class MopComponent : Component, IAfterInteract
    {
        public override string Name => "Mop";

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

        private string _pickupSound = "";

        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataFieldCached(ref _pickupSound, "pickup_sound", "/Audio/Effects/Fluids/slosh.ogg");
            // The turbo mop will pickup more
            serializer.DataFieldCached(ref _pickupAmount, "pickup_amount", ReagentUnit.New(5));
        }

        public override void Initialize()
        {
            base.Initialize();

            if (!Owner.EnsureComponent(out SolutionContainerComponent _))
            {
                Logger.Warning($"Entity {Owner.Name} at {Owner.Transform.MapPosition} didn't have a {nameof(SolutionContainerComponent)}");
            }
        }

        void IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (!Owner.TryGetComponent(out SolutionContainerComponent? contents)) return;
            if (!eventArgs.InRangeUnobstructed(ignoreInsideBlocker: true, popup: true)) return;

            if (CurrentVolume <= 0)
            {
                return;
            }

            if (eventArgs.Target == null)
            {
                // Drop the liquid on the mop on to the ground
                contents.SplitSolution(CurrentVolume).SpillAt(eventArgs.ClickLocation, "PuddleSmear");

                return;
            }

            if (!eventArgs.Target.TryGetComponent(out PuddleComponent? puddleComponent))
            {
                return;
            }
            // Essentially pickup either:
            // - _pickupAmount,
            // - whatever's left in the puddle, or
            // - whatever we can still hold (whichever's smallest)
            var transferAmount = ReagentUnit.Min(ReagentUnit.New(5), puddleComponent.CurrentVolume, CurrentVolume);
            bool puddleCleaned = puddleComponent.CurrentVolume - transferAmount <= 0;

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
                    return;
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

            // Give some visual feedback shit's happening (for anyone who can't hear sound)
            Owner.PopupMessage(eventArgs.User, Loc.GetString("Swish"));

            if (string.IsNullOrWhiteSpace(_pickupSound))
            {
                return;
            }

            EntitySystem.Get<AudioSystem>().PlayFromEntity(_pickupSound, Owner);
        }
    }
}
