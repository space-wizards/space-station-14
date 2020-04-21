using System;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.GameObjects.Components.Sound;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Fluids
{
    /// <summary>
    /// For cleaning up puddles
    /// </summary>
    [RegisterComponent]
    public class MopComponent : Component, IAfterAttack
    {
#pragma warning disable 649
        [Dependency] private readonly ILocalizationManager _localizationManager;
#pragma warning restore 649

        public override string Name => "Mop";
        internal SolutionComponent Contents => _contents;
        private SolutionComponent _contents;

        public ReagentUnit MaxVolume
        {
            get => _contents.MaxVolume;
            set => _contents.MaxVolume = value;
        }

        public ReagentUnit CurrentVolume => _contents.CurrentVolume;

        // Currently there's a separate amount for pickup and dropoff so
        // Picking up a puddle requires multiple clicks
        // Dumping in a bucket requires 1 click
        // Long-term you'd probably use a cooldown and start the pickup once we have some form of global cooldown
        public ReagentUnit PickupAmount => _pickupAmount;
        private ReagentUnit _pickupAmount;

        private string _pickupSound;

        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataFieldCached(ref _pickupSound, "pickup_sound", "/Audio/effects/Fluids/slosh.ogg");
            // The turbo mop will pickup more
            serializer.DataFieldCached(ref _pickupAmount, "pickup_amount", ReagentUnit.New(5));
        }

        public override void Initialize()
        {
            base.Initialize();
            _contents = Owner.GetComponent<SolutionComponent>();

        }

        void IAfterAttack.AfterAttack(AfterAttackEventArgs eventArgs)
        {
            Solution solution;
            if (eventArgs.Attacked == null)
            {
                if (CurrentVolume <= 0)
                {
                    return;
                }
                // Drop the liquid on the mop on to the ground I guess? Potentially change by design
                // Maybe even use a toggle mode instead of "Pickup" and "dropoff"
                solution = _contents.SplitSolution(CurrentVolume);
                SpillHelper.SpillAt(eventArgs.ClickLocation, solution, "PuddleSmear");

                return;
            }

            if (!eventArgs.Attacked.TryGetComponent(out PuddleComponent puddleComponent))
            {
                return;
            }
            // Essentially pickup either:
            // - _pickupAmount,
            // - whatever's left in the puddle, or
            // - whatever we can still hold (whichever's smallest)
            var transferAmount = ReagentUnit.Min(ReagentUnit.New(5), puddleComponent.CurrentVolume, MaxVolume - CurrentVolume);
            if (transferAmount == 0)
            {
                return;
            }

            solution = puddleComponent.SplitSolution(transferAmount);
            // Probably don't recolor a mop? Could work, if we layered it maybe
            if (!_contents.TryAddSolution(solution, false, true))
            {
                // I can't imagine why this would happen
                throw new InvalidOperationException();
            }

            // Give some visual feedback shit's happening (for anyone who can't hear sound)
            Owner.PopupMessage(eventArgs.User, _localizationManager.GetString("Swish"));

            if (_pickupSound == null)
            {
                return;
            }

            Owner.TryGetComponent(out SoundComponent soundComponent);
            soundComponent?.Play(_pickupSound);

        }
    }
}
