using Content.Client.Disposal.Unit;
using Content.Client.Power.EntitySystems;
using Content.Shared.Disposal.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using System.Linq;

namespace Content.Client.Disposal.Mailing;

public sealed class MailingUnitBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private MailingUnitWindow? _mailingUnitWindow;

    public MailingUnitBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    private void ButtonPressed(DisposalUnitUiButton button)
    {
        SendPredictedMessage(new DisposalUnitUiButtonPressedMessage(button));
    }

    private void TargetSelected(ItemList.ItemListSelectedEventArgs args)
    {
        var item = args.ItemList[args.ItemIndex];
        SendPredictedMessage(new TargetSelectedMessage(item.Text));
    }

    protected override void Open()
    {
        base.Open();

        _mailingUnitWindow = this.CreateWindow<MailingUnitWindow>();
        _mailingUnitWindow.OpenCenteredRight();

        _mailingUnitWindow.Eject.OnPressed += _ => ButtonPressed(DisposalUnitUiButton.Eject);
        _mailingUnitWindow.Engage.OnPressed += _ => ButtonPressed(DisposalUnitUiButton.Engage);
        _mailingUnitWindow.Power.OnPressed += _ => ButtonPressed(DisposalUnitUiButton.Power);

        _mailingUnitWindow.TargetListContainer.OnItemSelected += TargetSelected;

        if (EntMan.TryGetComponent(Owner, out MailingUnitComponent? component))
        {
            Refresh((Owner, component));
        }
    }

    public override void Update()
    {
        base.Update();

        if (EntMan.TryGetComponent(Owner, out MailingUnitComponent? component))
        {
            Refresh((Owner, component));
        }
    }

    public void Refresh(Entity<MailingUnitComponent> entity)
    {
        if (_mailingUnitWindow == null)
            return;

        _mailingUnitWindow.Title = string.IsNullOrEmpty(entity.Comp.Tag)
            ? Loc.GetString("ui-mailing-unit-window-title-unnamed")
            : Loc.GetString("ui-mailing-unit-window-title", ("tag", entity.Comp.Tag));
        _mailingUnitWindow.Target.Text = entity.Comp.Target;

        var entries = entity.Comp.TargetList.Select(target => new ItemList.Item(_mailingUnitWindow.TargetListContainer)
        {
            Text = target,
            Selected = target == entity.Comp.Target
        }).ToList();
        _mailingUnitWindow.TargetListContainer.SetItems(entries);

        if (!EntMan.TryGetComponent(entity.Owner, out DisposalUnitComponent? disposals))
            return;

        var disposalSystem = EntMan.System<DisposalUnitSystem>();
        var disposalState = disposalSystem.GetState((Owner, disposals));
        var fullPressure = disposalSystem.EstimatedFullPressure((Owner, disposals));
        var pressurePerSecond = disposals.PressurePerSecond;

        _mailingUnitWindow.UnitState.Text = Loc.GetString($"disposal-unit-state-{disposalState}");
        _mailingUnitWindow.FullPressure = fullPressure;
        _mailingUnitWindow.PressurePerSecond = pressurePerSecond;
        _mailingUnitWindow.PressureBar.UpdatePressure(fullPressure, pressurePerSecond);
        _mailingUnitWindow.Power.Pressed = EntMan.System<PowerReceiverSystem>().IsPowered(Owner);
        _mailingUnitWindow.Engage.Pressed = disposals.Engaged;
    }
}
