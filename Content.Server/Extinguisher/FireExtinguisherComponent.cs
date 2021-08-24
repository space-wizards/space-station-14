using System.Threading.Tasks;
using Content.Server.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Interaction;
using Content.Shared.Notification.Managers;
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

            if (eventArgs.Target.TryGetComponent(out ReagentTankComponent? tank)
                && solutionContainerSystem.TryGetDrainableSolution(eventArgs.Target, out var targetSolution)
                && solutionContainerSystem.TryGetDefaultSolution(Owner, out var container))
            {
                var trans = ReagentUnit.Min(container.EmptyVolume, targetSolution.DrainAvailable);
                if (trans > 0)
                {
                    var drained = solutionContainerSystem.Drain(targetSolution, trans);
                    solutionContainerSystem.TryAddSolution(container, drained);

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
