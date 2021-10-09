using System.Threading.Tasks;
using Content.Server.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Sound;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Extinguisher
{
    [RegisterComponent]
    public class FireExtinguisherComponent : Component, IAfterInteract
    {
        public override string Name => "FireExtinguisher";

        [DataField("refillSound")] SoundSpecifier _refillSound = new SoundPathSpecifier("/Audio/Effects/refill.ogg");

        // Higher priority than sprays.
        int IAfterInteract.Priority => 1;

        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            var solutionContainerSystem = EntitySystem.Get<SolutionContainerSystem>();
            if (eventArgs.Target == null || !eventArgs.CanReach)
            {
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
                        Loc.GetString("fire-extinguisher-component-after-interact-refilled-message", ("owner", Owner)));
                }

                return true;
            }

            return false;
        }
    }
}
