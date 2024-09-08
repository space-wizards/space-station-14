using Content.Shared.Clothing.Components;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;

namespace Content.Client.Clothing.UI;

public sealed class ToggleableClothingBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IClyde _displayManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;

    private IEntityManager _entityManager;
    private ToggleableClothingRadialMenu? _menu;

    public ToggleableClothingBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
        _entityManager = IoCManager.Resolve<IEntityManager>();
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<ToggleableClothingRadialMenu>();
        _menu.SetEntity(Owner);
        _menu.SendToggleClothingMessageAction += SendToggleableClothingMessage;

        var vpSize = _displayManager.ScreenSize;
        _menu.OpenCenteredAt(_inputManager.MouseScreenPosition.Position / vpSize);
    }

    private void SendToggleableClothingMessage(EntityUid uid)
    {
        var message = new ToggleableClothingUiMessage(_entityManager.GetNetEntity(uid));
        SendPredictedMessage(message);
    }
}
