using Content.Client.UserInterface.Systems.Actions;
using Content.Client.UserInterface.Systems.Admin;
using Content.Client.UserInterface.Systems.Bwoink;
using Content.Client.UserInterface.Systems.Character;
using Content.Client.UserInterface.Systems.Crafting;
using Content.Client.UserInterface.Systems.Emotes;
using Content.Client.UserInterface.Systems.EscapeMenu;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Client.UserInterface.Systems.Guidebook;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Client.UserInterface.Systems.Sandbox;
using Content.Client._Starlight.UserInterface.Systems.Language; // Starlight
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.UserInterface.Systems.MenuBar;

public sealed class GameTopMenuBarUIController : UIController
{
    [Dependency] private readonly EscapeUIController _escape = default!;
    [Dependency] private readonly AdminUIController _admin = default!;
    [Dependency] private readonly CharacterUIController _character = default!;
    [Dependency] private readonly CraftingUIController _crafting = default!;
    [Dependency] private readonly AHelpUIController _ahelp = default!;
    [Dependency] private readonly MHelpUIController _mhelp = default!; //🌟Starlight🌟
    [Dependency] private readonly ActionUIController _action = default!;
    [Dependency] private readonly SandboxUIController _sandbox = default!;
    [Dependency] private readonly GuidebookUIController _guidebook = default!;
    [Dependency] private readonly EmotesUIController _emotes = default!;
    [Dependency] private readonly LanguageMenuUIController _language = default!; // Starlight

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
        _admin.UnloadButton();
        _character.UnloadButton();
        _crafting.UnloadButton();
        _ahelp.UnloadButton();
        _mhelp.UnloadButton(); //🌟Starlight🌟
        _action.UnloadButton();
        _sandbox.UnloadButton();
        _emotes.UnloadButton();
        _language.UnloadButton(); // Starlight
    }

    public void LoadButtons()
    {
        _escape.LoadButton();
        _guidebook.LoadButton();
        _admin.LoadButton();
        _character.LoadButton();
        _crafting.LoadButton();
        _ahelp.LoadButton();
        _mhelp.LoadButton(); //🌟Starlight🌟
        _action.LoadButton();
        _sandbox.LoadButton();
        _emotes.LoadButton();
        _language.LoadButton(); // Starlight
    }
}
