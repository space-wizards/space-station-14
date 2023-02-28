using Content.Client.Gameplay;
using Content.Client.UserInterface.Systems.Actions;
using Content.Client.UserInterface.Systems.Alerts;
using Content.Client.UserInterface.Systems.Chat;
using Content.Client.UserInterface.Systems.Ghost;
using Content.Client.UserInterface.Systems.Hotbar;
using Content.Client.UserInterface.Systems.MenuBar;
using Content.Client.UserInterface.Systems.Viewport;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.UserInterface.Systems.Gameplay;

public sealed class GameplayStateLoadController : UIController, IOnStateChanged<GameplayState>
{
    public Action? OnScreenLoad;
    public Action? OnScreenUnload;

    public void OnStateEntered(GameplayState state)
    {
        LoadScreen();
    }

    public void OnStateExited(GameplayState state)
    {
        UnloadScreen();
    }

    public void UnloadScreen()
    {
        OnScreenUnload!();
    }

    public void LoadScreen()
    {
        OnScreenLoad!();
    }
}
