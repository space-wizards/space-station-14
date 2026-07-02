using Content.Shared.Ame;
using Content.Shared.Ame.Components;
using Content.Shared.Ame.Systems;
using Content.Shared.NodeContainer.Systems;
using Content.Shared.Power.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Ame.UI;

[UsedImplicitly]
public sealed partial class AmeControllerBoundUserInterface : BoundUserInterface
{
    [UISystemDependency] private AmeNodeGroupHandler _ameHandler = default!;
    [UISystemDependency] private NodeContainerSystem _nodeContainer = default!;

    private AmeWindow? _window;

    public AmeControllerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<AmeWindow>();
        _window.OnAmeEjectButton += AmeControllerEjectPressed;
        _window.OnAmeToggleInjectionButton += AmeControllerToggleInjectionPressed;
        _window.OnAmeIncreaseFuelButton += AmeControllerIncreaseFuelPressed;
        _window.OnAmeDecreaseFuelButton += AmeControllerDecreaseFuelPressed;
    }

    // Used only for setting the current power supply
    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not AmeControllerBoundUserInterfaceState castState)
            return;

        _window?.UpdateState(castState);
    }

    public void AmeControllerEjectPressed()
    {
        SendPredictedMessage(new AmeControllerEjectMessage());
    }

    public void AmeControllerToggleInjectionPressed()
    {
        SendPredictedMessage(new AmeControllerToggleInjectionMessage());
    }

    public void AmeControllerIncreaseFuelPressed()
    {
        SendPredictedMessage(new AmeControllerIncreaseFuelMessage());
    }

    public void AmeControllerDecreaseFuelPressed()
    {
        SendPredictedMessage(new AmeControllerDecreaseFuelMessage());
    }

    /// <summary>
    /// Updates the window using client-side data.
    /// </summary>
    public override void Update()
    {
        if (!EntMan.TryGetComponent<AmeControllerComponent>(Owner, out var controller))
            return;

        var isMaster = false;
        var powered = !EntMan.TryGetComponent<PowerReceiverComponent>(Owner, out var powerSource) || powerSource.Powered;
        var coreCount = 0;
        // how much power can be produced at the current settings, in kW
        // we don't use max. here since this is what is set in the Controller, not what the AME is actually producing
        float targetedPowerSupply = 0;
        if (_nodeContainer.TryGetFirstNodeGroup<AmeNodeGroup>(Owner, out var group) && group.Cores.Count > 0)
        {
            isMaster = group.MasterController == Owner;
            coreCount = group.Cores.Count;
            targetedPowerSupply = _ameHandler.CalculatePower(controller.InjectionAmount, group.Cores.Count) / 1000;
        }

        var fuelContainerInSlot = controller.FuelSlot.Item;
        var hasFuelContainerInSlot = EntMan.EntityExists(fuelContainerInSlot);
        var fuelAmount = 0;
        if (EntMan.TryGetComponent<AmeFuelContainerComponent>(fuelContainerInSlot, out var fuelContainer))
            fuelAmount = fuelContainer.FuelAmount;

        _window?.SetPowered(powered);
        _window?.SetIsMaster(isMaster);
        _window?.SetFuelJar(hasFuelContainerInSlot, fuelAmount);
        _window?.SetInjectionStatus(controller.Injecting);
        _window?.SetAmeStats(coreCount, controller.InjectionAmount, targetedPowerSupply);
    }
}
