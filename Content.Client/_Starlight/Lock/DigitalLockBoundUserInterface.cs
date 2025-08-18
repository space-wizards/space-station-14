using Content.Shared.Containers.ItemSlots;
using Content.Shared._Starlight.Lock;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client._Starlight.Lock;

[UsedImplicitly]
public sealed class DigitalLockBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private DigitalLockMenu? _menu;

    public DigitalLockBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<DigitalLockMenu>();

        _menu.OnKeypadButtonPressed += i =>
        {
            SendMessage(new DigitalLockKeypadMessage(i));
        };
        _menu.OnEnterButtonPressed += () =>
        {
            SendMessage(new DigitalLockKeypadEnterMessage());
        };
        _menu.OnClearButtonPressed += () =>
        {
            SendMessage(new DigitalLockKeypadClearMessage());
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_menu == null)
            return;

        switch (state)
        {
            case DigitalLockUiState msg:
                _menu.UpdateState(msg);
                break;
        }
    }
}