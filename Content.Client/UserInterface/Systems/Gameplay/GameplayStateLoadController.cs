using Content.Client.Gameplay;
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
        OnScreenUnload?.Invoke();
    }

    public void LoadScreen()
    {
        OnScreenLoad?.Invoke();
    }
}
