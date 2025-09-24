using Content.Client.Power.EntitySystems;
using Content.Shared.Disposal.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Disposal.Unit;

[UsedImplicitly]
public sealed class DisposalUnitBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private DisposalUnitWindow? _disposalUnitWindow;

    public DisposalUnitBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    private void ButtonPressed(DisposalUnitUiButton button)
    {
        SendPredictedMessage(new DisposalUnitUiButtonPressedMessage(button));
    }

    protected override void Open()
    {
        base.Open();

        _disposalUnitWindow = this.CreateWindow<DisposalUnitWindow>();
        _disposalUnitWindow.OpenCenteredRight();

        _disposalUnitWindow.Eject.OnPressed += _ => ButtonPressed(DisposalUnitUiButton.Eject);
        _disposalUnitWindow.Engage.OnPressed += _ => ButtonPressed(DisposalUnitUiButton.Engage);
        _disposalUnitWindow.Power.OnPressed += _ => ButtonPressed(DisposalUnitUiButton.Power);

        if (EntMan.TryGetComponent(Owner, out DisposalUnitComponent? component))
        {
            Refresh((Owner, component));
        }
    }

    public void Refresh(Entity<DisposalUnitComponent> entity)
    {
        if (_disposalUnitWindow == null)
            return;

        _disposalUnitWindow.Title = EntMan.GetComponent<MetaDataComponent>(entity.Owner).EntityName;

        if (!EntMan.TryGetComponent(entity.Owner, out DisposalUnitComponent? disposals))
            return;

        var disposalUnit = EntMan.System<DisposalUnitSystem>();
        var disposalState = disposalUnit.GetState(entity);
        var fullPressure = disposalUnit.EstimatedFullPressure((Owner, disposals));
        var pressurePerSecond = disposals.PressurePerSecond;

        _disposalUnitWindow.UnitState.Text = Loc.GetString($"disposal-unit-state-{disposalState}");
        _disposalUnitWindow.FullPressure = disposalUnit.EstimatedFullPressure(entity);
        _disposalUnitWindow.PressurePerSecond = entity.Comp.PressurePerSecond;
        _disposalUnitWindow.PressureBar.UpdatePressure(fullPressure, pressurePerSecond);
        _disposalUnitWindow.Power.Pressed = EntMan.System<PowerReceiverSystem>().IsPowered(Owner);
        _disposalUnitWindow.Engage.Pressed = entity.Comp.Engaged;
    }
}
