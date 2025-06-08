using Content.Client.Disposal.Unit;
using Content.Client.Power.EntitySystems;
using Content.Shared.Disposal;
using Content.Shared.Disposal.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Disposal.Mailing;

public sealed class MailingUnitBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    public MailingUnitWindow? MailingUnitWindow;

    public MailingUnitBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    private void ButtonPressed(DisposalUnitComponent.UiButton button)
    {
        SendMessage(new DisposalUnitComponent.UiButtonPressedMessage(button));
        // If we get client-side power stuff then we can predict the button presses but for now we won't as it stuffs
        // the pressure lerp up.
    }

    private void TargetSelected(ItemList.ItemListSelectedEventArgs args)
    {
        var item = args.ItemList[args.ItemIndex];
        SendMessage(new TargetSelectedMessage(item.Text));
    }

    protected override void Open()
    {
        base.Open();

        MailingUnitWindow = this.CreateWindow<MailingUnitWindow>();
        MailingUnitWindow.OpenCenteredRight();

        MailingUnitWindow.Eject.OnPressed += _ => ButtonPressed(DisposalUnitComponent.UiButton.Eject);
        MailingUnitWindow.Engage.OnPressed += _ => ButtonPressed(DisposalUnitComponent.UiButton.Engage);
        MailingUnitWindow.Power.OnPressed += _ => ButtonPressed(DisposalUnitComponent.UiButton.Power);

        MailingUnitWindow.TargetListContainer.OnItemSelected += TargetSelected;

        if (EntMan.TryGetComponent(Owner, out MailingUnitComponent? component))
            Refresh((Owner, component));
    }

    public void Refresh(Entity<MailingUnitComponent> entity)
    {
        if (MailingUnitWindow == null)
            return;

        // TODO: This should be decoupled from disposals
        if (EntMan.TryGetComponent(entity.Owner, out DisposalUnitComponent? disposals))
        {
            var disposalSystem = EntMan.System<DisposalUnitSystem>();

            var disposalState = disposalSystem.GetState(Owner, disposals);
            var fullPressure = disposalSystem.EstimatedFullPressure(Owner, disposals);

            MailingUnitWindow.UnitState.Text = Loc.GetString($"disposal-unit-state-{disposalState}");
            MailingUnitWindow.FullPressure = fullPressure;
            MailingUnitWindow.PressureBar.UpdatePressure(fullPressure);
            MailingUnitWindow.Power.Pressed = EntMan.System<PowerReceiverSystem>().IsPowered(Owner);
            MailingUnitWindow.Engage.Pressed = disposals.Engaged;
        }

        MailingUnitWindow.Title = Loc.GetString("ui-mailing-unit-window-title", ("tag", entity.Comp.Tag ?? " "));
        //UnitTag.Text = state.Tag;
        MailingUnitWindow.Target.Text = entity.Comp.Target;

        MailingUnitWindow.TargetListContainer.Clear();
        foreach (var target in entity.Comp.TargetList)
        {
            MailingUnitWindow.TargetListContainer.AddItem(target);
        }
    }
}
