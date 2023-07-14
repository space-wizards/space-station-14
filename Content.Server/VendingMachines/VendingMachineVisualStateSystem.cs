using Content.Server.Power.EntitySystems;
using Content.Shared.Broke;
using Content.Shared.VendingMachines;
using Content.Shared.VendingMachines.Components;
using Robust.Server.GameObjects;

namespace Content.Server.VendingMachines;

public sealed class VendingMachineVisualStateSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;

    /// <summary>
    /// Tries to update the visual elements of the component,
    /// if there is a visualization component, based on its current state.
    /// </summary>
    public void UpdateVisualState(EntityUid uid)
    {
        if (!TryComp<VendingMachineVisualStateComponent>(uid, out var visualStateComponent))
        {
            return;
        }

        TryUpdateVisualState(uid, visualStateComponent);
    }

    /// <summary>
    /// Tries to update the visuals of the component based on its current state.
    /// </summary>
    public void TryUpdateVisualState(EntityUid uid,
        VendingMachineVisualStateComponent? vendComponent = null)
    {
        if (!Resolve(uid, ref vendComponent))
            return;

        if (!TryComp<VendingMachineEjectComponent>(uid, out var ejectComponent))
            return;

        if (!TryComp<BrokeComponent>(uid, out var brokeComponent))
            return;

        var finalState = VendingMachineVisualState.Normal;

        if (brokeComponent.Broken)
        {
            finalState = VendingMachineVisualState.Broken;
        }
        else if (ejectComponent.Ejecting)
        {
            finalState = VendingMachineVisualState.Eject;
        }
        else if (ejectComponent.Denying)
        {
            finalState = VendingMachineVisualState.Deny;
        }
        else if (!this.IsPowered(uid, EntityManager))
        {
            finalState = VendingMachineVisualState.Off;
        }

        _appearanceSystem.SetData(uid, VendingMachineVisuals.VisualState, finalState);
    }
}
