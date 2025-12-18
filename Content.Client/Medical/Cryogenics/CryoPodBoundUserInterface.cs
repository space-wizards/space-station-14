using Content.Shared.FixedPoint;
using Content.Shared.Medical.Cryogenics;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Medical.Cryogenics;

[UsedImplicitly]
public sealed class CryoPodBoundUserInterface : BoundUserInterface
{
    private CryoPodWindow? _window;

    public CryoPodBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindowCenteredLeft<CryoPodWindow>();
        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
        _window.OnEjectPressed += EjectPressed;
        _window.OnInjectPressed += InjectPressed;
    }

    private void EjectPressed()
    {
        bool isLocked =
            EntMan.TryGetComponent<CryoPodComponent>(Owner, out var cryoComp)
            && cryoComp.Locked;

        _window?.SetEjectErrorVisible(isLocked);
        SendMessage(new CryoPodUiMessage(CryoPodUiMessage.MessageType.Eject));
    }

    private void InjectPressed(FixedPoint2 transferAmount)
    {
        SendMessage(new CryoPodUiMessage(CryoPodUiMessage.MessageType.Inject, transferAmount));
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        if (_window != null && message is CryoPodUserMessage cryoMsg)
        {
            _window.Populate(cryoMsg);
        }
    }
}
