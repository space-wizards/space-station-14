using System.Threading.Tasks;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.ActionBlocker;
using Content.Shared.Audio;
using Content.Shared.Extinguisher;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Sound;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Extinguisher
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(SharedFireExtinguisherComponent))]
    public class FireExtinguisherComponent : SharedFireExtinguisherComponent, IAfterInteract, IActivate, IDropped
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

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

            if (eventArgs.Target is not {Valid: true} target ||
                !_entMan.HasComponent<ReagentTankComponent>(target) ||
                !solutionContainerSystem.TryGetDrainableSolution(target, out var targetSolution) ||
                !solutionContainerSystem.TryGetDrainableSolution(Owner, out var container))
            {
                return false;
            }

            var transfer = FixedPoint2.Min(container.AvailableVolume, targetSolution.DrainAvailable);
            if (transfer > 0)
            {
                var drained = solutionContainerSystem.Drain(target, targetSolution, transfer);
                solutionContainerSystem.TryAddSolution(Owner, container, drained);

                SoundSystem.Play(Filter.Pvs(Owner), _refillSound.GetSound(), Owner);
                eventArgs.Target.Value.PopupMessage(eventArgs.User,
                    Loc.GetString("fire-extingusiher-component-after-interact-refilled-message", ("owner", Owner)));
            }

            return true;

        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            ToggleSafety(eventArgs.User);
        }

        private void ToggleSafety(EntityUid user)
        {
            SoundSystem.Play(Filter.Pvs(Owner), SafetySound.GetSound(), Owner, AudioHelpers.WithVariation(0.125f).WithVolume(-4f));
            SetSafety(user, !_safety);
        }

        private void SetSafety(EntityUid user, bool state)
        {
            if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user) || !_hasSafety)
                return;

            _safety = state;

            if (_entMan.TryGetComponent(Owner, out AppearanceComponent? appearance))
                appearance.SetData(FireExtinguisherVisuals.Safety, _safety);
        }

        void IDropped.Dropped(DroppedEventArgs eventArgs)
        {
            if (_hasSafety && _entMan.TryGetComponent(Owner, out AppearanceComponent? appearance))
                appearance.SetData(FireExtinguisherVisuals.Safety, _safety);
        }
    }
}
