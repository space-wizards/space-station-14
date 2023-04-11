using Content.Client.UserInterface.Systems.Info;
using Content.Shared.Input;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;

namespace Content.Client.UserInterface.Systems.EscapeMenu;

public sealed class EscapeContextUIController : UIController
{
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;

    public override void Initialize()
    {
        _inputManager.SetInputCommand(ContentKeyFunctions.EscapeContext,
            InputCmdHandler.FromDelegate(_ => Esc()));
    }

    private void Esc()
    {
        var closeRecentWindowUIController = _userInterfaceManager.GetUIController<CloseRecentWindowUIController>();
        var escapeUIController = _userInterfaceManager.GetUIController<EscapeUIController>();

        if (closeRecentWindowUIController.HasClosableWindows())
        {
            closeRecentWindowUIController.CloseMostRecentWindow();
        }
        else
        {
            escapeUIController.ToggleWindow();
        }
    }
}
