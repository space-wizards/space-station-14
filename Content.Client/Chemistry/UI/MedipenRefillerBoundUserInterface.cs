using Content.Shared.Chemistry;
using Content.Client.Chemistry.Components;
using Content.Shared.Containers.ItemSlots;

namespace Content.Client.Chemistry.UI;
public sealed class MedipenRefillerBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private MedipenRefillerWindow? _window;

    public MedipenRefillerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }
    protected override void Open()
    {
        base.Open();

        _window = new MedipenRefillerWindow
        {
            Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName,
        };

        var component = EntMan.GetComponent<MedipenRefillerComponent>(Owner);

        _window.OpenCentered();
        _window.OnClose += Close;
        _window.OnMedipenButtonPressed += id => SendMessage(new MedipenRefillerActivateMessage(id));
        _window.OnTransferButtonPressed += args => SendMessage(new MedipenRefillerTransferReagentMessage(args.Id, args.Value, args.IsBuffer));
        _window.InputEjectButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent(component.InputSlotName));
        _window.MedipenEjectButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent(component.MedipenSlotName));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        switch (state)
        {
            case MedipenRefillerUpdateState msg:
                if (_window != null)
                {
                    _window.InputContainerData = msg.InputContainerData;
                    _window.BufferData = msg.BufferData;
                    _window.IsActivated = msg.IsActivated;
                    _window.CurrentRecipe = msg.CurrentRecipe;
                    _window.RemainingTime = msg.RemainingTime;
                }
                _window?.UpdateRecipes();
                _window?.UpdateContainerInfo();
                break;
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _window?.Dispose();
        }
    }
}
