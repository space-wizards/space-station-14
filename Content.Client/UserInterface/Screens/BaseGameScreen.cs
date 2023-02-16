using Content.Client.UserInterface.Systems.Chat.Widgets;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Screens;

public abstract class BaseGameScreen : UIScreen
{
    public Action<Vector2>? OnChatResized;

    public abstract ChatBox ChatBox { get; }

    public abstract ScreenType ScreenType { get; }
}
