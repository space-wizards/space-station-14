using Content.Client.Administration.Managers;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Robust.Client.Console;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.UserInterface.Systems.MenuBar;

public sealed class GameTopMenuBarUIController : UIController
{
    [Dependency] private readonly IClientAdminManager _admin = default!;
    [Dependency] private readonly IClientConGroupController _conGroups = default!;

    public override void Initialize()
    {
        base.Initialize();

        _admin.AdminStatusUpdated += OnAdminStatusUpdated;
    }

    private void OnAdminStatusUpdated()
    {
        var button = UIManager.GetActiveUIWidgetOrNull<GameTopMenuBar>()?.AdminButton;

        if (button != null)
            button.Visible = _conGroups.CanAdminMenu();
    }
}
