using System;
using System.Threading.Tasks;
using Content.Server.Chemistry.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Audio;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Sound;
using Content.Shared.Extinguisher;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Extinguisher
{
    [RegisterComponent]
    public class FireExtinguisherComponent : SharedFireExtinguisherComponent, IAfterInteract, IUse, IActivate, IDropped
    {
        public override string Name => "FireExtinguisher";

        [DataField("refillSound")]
        SoundSpecifier _refillSound = new SoundPathSpecifier("/Audio/Effects/refill.ogg");
        [DataField("hasSafety")]
        private bool _hasSafety;
        [DataField("safety")]
        private bool _safety = true;
        [DataField("safetySound")]
        public SoundSpecifier SafetySound { get; } = new SoundPathSpecifier("/Audio/Machines/button.ogg");

        // Higher priority than sprays.
        int IAfterInteract.Priority => 1;

        protected override void Initialize()
        {
            base.Initialize();

            if (_hasSafety)
            {
                SetSafety(Owner, _safety);
            }
        }

        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            var solutionContainerSystem = EntitySystem.Get<SolutionContainerSystem>();
            if (eventArgs.Target == null || !eventArgs.CanReach)
            {
                if (_hasSafety && _safety)
                {
                    Owner.PopupMessage(eventArgs.User, Loc.GetString("fire-extinguisher-component-safety-on-message"));
                    return true;
                }
                return false;
            }

            var targetEntity = eventArgs.Target;
            if (eventArgs.Target.HasComponent<ReagentTankComponent>()
                && solutionContainerSystem.TryGetDrainableSolution(targetEntity.Uid, out var targetSolution)
                && solutionContainerSystem.TryGetDrainableSolution(Owner.Uid, out var container))
            {
                var transfer = ReagentUnit.Min(container.AvailableVolume, targetSolution.DrainAvailable);
                if (transfer > 0)
                {
                    var drained = solutionContainerSystem.Drain(targetEntity.Uid, targetSolution, transfer);
                    solutionContainerSystem.TryAddSolution(Owner.Uid, container, drained);

                    SoundSystem.Play(Filter.Pvs(Owner), _refillSound.GetSound(), Owner);
                    eventArgs.Target.PopupMessage(eventArgs.User,
                        Loc.GetString("fire-extingusiher-component-after-interact-refilled-message", ("owner", Owner)));
                }

                return true;
            }

            return false;
        }
        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            ToggleSafety(eventArgs.User);
            return true;
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            ToggleSafety(eventArgs.User);
        }

        private void ToggleSafety(IEntity user)
        {
            SoundSystem.Play(Filter.Pvs(Owner), SafetySound.GetSound(), Owner, AudioHelpers.WithVariation(0.125f).WithVolume(-4f));
            SetSafety(user, !_safety);
        }

        private void SetSafety(IEntity user, bool state)
        {
            if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user) || !_hasSafety)
                return;

            _safety = state;

            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
                appearance.SetData(FireExtinguisherVisuals.Safety, _safety);
        }

        void IDropped.Dropped(DroppedEventArgs eventArgs)
        {
            if (_hasSafety && Owner.TryGetComponent(out AppearanceComponent? appearance))
                appearance.SetData(FireExtinguisherVisuals.Safety, _safety);
        }
    }
}
