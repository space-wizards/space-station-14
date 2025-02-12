using System.Numerics;
using Content.Client.UserInterface.Systems.Chat.Widgets;
using Robust.Client.UserInterface;

namespace Content.Client.UserInterface.Screens;

/// <summary>
///     Screens that are considered to be 'in-game'.
/// </summary>
public abstract class InGameScreen : UIScreen
{
    public Action<Vector2>? OnChatResized;

    public abstract ChatBox ChatBox { get; }

    public abstract void SetChatSize(Vector2 size);
}
