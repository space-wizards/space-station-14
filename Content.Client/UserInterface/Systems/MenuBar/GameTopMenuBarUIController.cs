using Content.Client.Gameplay;
using Content.Client.UserInterface.Systems.Actions;
using Content.Client.UserInterface.Systems.Admin;
using Content.Client.UserInterface.Systems.Bwoink;
using Content.Client.UserInterface.Systems.Character;
using Content.Client.UserInterface.Systems.Crafting;
using Content.Client.UserInterface.Systems.Emotes;
using Content.Client.UserInterface.Systems.Guidebook;
using Content.Client.UserInterface.Systems.Sandbox;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.UserInterface.Systems.MenuBar;

public sealed class GameTopMenuBarUIController : UIController, IOnStateChanged<GameplayState>
{
    [Dependency] private readonly ActionUIController _action = default!;
    [Dependency] private readonly AdminUIController _admin = default!;
    [Dependency] private readonly AHelpUIController _ahelp = default!;
    [Dependency] private readonly CharacterUIController _character = default!;
    [Dependency] private readonly CraftingUIController _crafting = default!;
    [Dependency] private readonly EmotesUIController _emotes = default!;
    [Dependency] private readonly GuidebookUIController _guidebook = default!;
    [Dependency] private readonly SandboxUIController _sandbox = default!;

    public void OnStateExited(GameplayState state)
    {
        _guidebook.UnloadButton();
        _admin.UnloadButton();
        _character.UnloadButton();
        _crafting.UnloadButton();
        _ahelp.UnloadButton();
        _action.UnloadButton();
        _sandbox.UnloadButton();
        _emotes.UnloadButton();
    }

    public void OnStateEntered(GameplayState state)
    {
        _guidebook.LoadButton();
        _admin.LoadButton();
        _character.LoadButton();
        _crafting.LoadButton();
        _ahelp.LoadButton();
        _action.LoadButton();
        _sandbox.LoadButton();
        _emotes.LoadButton();
    }
}
