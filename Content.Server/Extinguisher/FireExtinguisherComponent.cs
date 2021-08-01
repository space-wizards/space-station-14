using System.Threading.Tasks;
using Content.Server.Chemistry.Components;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Solution.Components;
using Content.Shared.Interaction;
using Content.Shared.Notification.Managers;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;

namespace Content.Server.Extinguisher
{
    [RegisterComponent]
    public class FireExtinguisherComponent : Component, IAfterInteract
    {
        public override string Name => "FireExtinguisher";

        // Higher priority than sprays.
        int IAfterInteract.Priority => 1;

        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (eventArgs.Target == null || !eventArgs.CanReach)
            {
                return false;
            }

            if (eventArgs.Target.TryGetComponent(out ReagentTankComponent? tank)
                && eventArgs.Target.TryGetComponent(out SolutionContainerComponent? targetSolution)
                && targetSolution.CanDrain
                && Owner.TryGetComponent(out SolutionContainerComponent? container))
            {
                var chemistrySystem = EntitySystem.Get<ChemistrySystem>();
                var trans = ReagentUnit.Min(container.EmptyVolume, targetSolution.DrainAvailable);
                if (trans > 0)
                {
                    var drained = chemistrySystem.Drain(targetSolution, trans);
                    chemistrySystem.TryAddSolution(container, drained);

                    SoundSystem.Play(Filter.Pvs(Owner), "/Audio/Effects/refill.ogg", Owner);
                    eventArgs.Target.PopupMessage(eventArgs.User,
                        Loc.GetString("fire-extinguisher-component-after-interact-refilled-message", ("owner", Owner)));
                }

                return true;
            }

            return false;
        }
    }
}
