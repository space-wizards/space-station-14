using Content.Client.UserInterface.Systems.Actions;
using Content.Client.UserInterface.Systems.Admin;
using Content.Client.UserInterface.Systems.Bwoink;
using Content.Client.UserInterface.Systems.Character;
using Content.Client.UserInterface.Systems.Crafting;
using Content.Client.UserInterface.Systems.EscapeMenu;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Client.UserInterface.Systems.Guidebook;
using Content.Client.UserInterface.Systems.Inventory;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Client.UserInterface.Systems.Sandbox;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.UserInterface.Systems.MenuBar;

public sealed partial class GameTopMenuBarUIController : UIController
{
    [Dependency] private EscapeUIController _escape = default!;
    [Dependency] private InventoryUIController _inventory = default!;
    [Dependency] private AdminUIController _admin = default!;
    [Dependency] private CharacterUIController _character = default!;
    [Dependency] private CraftingUIController _crafting = default!;
    [Dependency] private AHelpUIController _ahelp = default!;
    [Dependency] private ActionUIController _action = default!;
    [Dependency] private SandboxUIController _sandbox = default!;
    [Dependency] private GuidebookUIController _guidebook = default!;

    private GameTopMenuBar? GameTopMenuBar => UIManager.GetActiveUIWidgetOrNull<GameTopMenuBar>();

    public override void Initialize()
    {
        base.Initialize();

        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += LoadButtons;
        gameplayStateLoad.OnScreenUnload += UnloadButtons;
    }

    public void UnloadButtons()
    {
        _escape.UnloadButton();
        _guidebook.UnloadButton();
        _inventory.UnloadButton();
        _admin.UnloadButton();
        _character.UnloadButton();
        _crafting.UnloadButton();
        _ahelp.UnloadButton();
        _action.UnloadButton();
        _sandbox.UnloadButton();
    }

    public void LoadButtons()
    {
        _escape.LoadButton();
        _guidebook.LoadButton();
        _inventory.LoadButton();
        _admin.LoadButton();
        _character.LoadButton();
        _crafting.LoadButton();
        _ahelp.LoadButton();
        _action.LoadButton();
        _sandbox.LoadButton();
    }
}
