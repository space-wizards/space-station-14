using Content.Client.Disposal.Mailing;
using Content.Client.Power.EntitySystems;
using Content.Shared.Disposal.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Disposal.Unit;

/// <summary>
/// Initializes a <see cref="MailingUnitWindow"/> or a <see cref="_disposalUnitWindow"/> and updates it when new server messages are received.
/// </summary>
[UsedImplicitly]
public sealed class DisposalUnitBoundUserInterface : BoundUserInterface
{
    [ViewVariables] private DisposalUnitWindow? _disposalUnitWindow;

    public DisposalUnitBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    private void ButtonPressed(DisposalUnitUiButton button)
    {
        SendPredictedMessage(new DisposalUnitUiButtonPressedMessage(button));
        // If we get client-side power stuff then we can predict the button presses but for now we won't as it stuffs
        // the pressure lerp up.
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

        var disposalUnit = EntMan.System<DisposalUnitSystem>();

        _disposalUnitWindow.Title = EntMan.GetComponent<MetaDataComponent>(entity.Owner).EntityName;

        var state = disposalUnit.GetState(entity);

        _disposalUnitWindow.UnitState.Text = Loc.GetString($"disposal-unit-state-{state}");
        _disposalUnitWindow.Power.Pressed = EntMan.System<PowerReceiverSystem>().IsPowered(Owner);
        _disposalUnitWindow.Engage.Pressed = entity.Comp.Engaged;
        _disposalUnitWindow.FullPressure = disposalUnit.EstimatedFullPressure(entity);
        _disposalUnitWindow.PressurePerSecond = entity.Comp.PressurePerSecond;
    }
}
