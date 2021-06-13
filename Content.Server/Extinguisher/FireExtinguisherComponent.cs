using System.Threading.Tasks;
using Content.Server.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Solution.Components;
using Content.Shared.Interaction;
using Content.Shared.Notification;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;

#nullable enable

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
                && eventArgs.Target.TryGetComponent(out ISolutionInteractionsComponent? targetSolution)
                && targetSolution.CanDrain
                && Owner.TryGetComponent(out SolutionContainerComponent? container))
            {
                var trans = ReagentUnit.Min(container.EmptyVolume, targetSolution.DrainAvailable);
                if (trans > 0)
                {
                    var drained = targetSolution.Drain(trans);
                    container.TryAddSolution(drained);

                    SoundSystem.Play(Filter.Pvs(Owner), "/Audio/Effects/refill.ogg", Owner);
                    eventArgs.Target.PopupMessage(eventArgs.User, Loc.GetString("{0:TheName} is now refilled", Owner));
                }

                return true;
            }

            return false;
        }
    }
}
