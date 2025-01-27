using Content.Shared.Heretic;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;

namespace Content.Client._Goobstation.Heretic.UI;

public sealed partial class LivingHeartMenuBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IClyde _displayManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;

    [NonSerialized] private LivingHeartMenu? _menu;

    public LivingHeartMenuBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<LivingHeartMenu>();
        _menu.SetEntity(Owner);
        _menu.SendActivateMessageAction += SendMessage;
        _menu.OpenCenteredAt(_inputManager.MouseScreenPosition.Position / _displayManager.ScreenSize);
    }

    private void SendMessage(NetEntity netent)
    {
        base.SendMessage(new EventHereticLivingHeartActivate() { Target = netent });
    }
}
