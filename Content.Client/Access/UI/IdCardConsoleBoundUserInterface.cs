using Content.Shared.Containers.ItemSlots;
using Content.Shared.CrewManifest;
using Robust.Client.UserInterface;
using static Content.Shared.Access.Components.IdCardConsoleComponent;

namespace Content.Client.Access.UI;

public sealed partial class IdCardConsoleBoundUserInterface : BoundUserInterface
{
    private IdCardConsoleWindow? _window;

    public IdCardConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<IdCardConsoleWindow>();
        _window.SetOwner(Owner);

        _window.OnDataSubmitted += SubmitData;
        _window.CrewManifestButton.OnPressed += _ => SendMessage(new CrewManifestOpenUiMessage());
        _window.PrivilegedIdButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent(PrivilegedIdCardSlotId));
        _window.TargetIdButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent(TargetIdCardSlotId));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        var castState = (IdCardConsoleBoundUserInterfaceState)state;
        _window?.UpdateState(castState);
    }

    public void SubmitData(IdCardData data)
    {
        SendMessage(new WriteToTargetIdMessage(
            data.FullName,
            data.JobTitle,
            data.Accesses,
            data.JobPrototype));
    }
}

