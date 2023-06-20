using Content.Client.Gameplay;
using Content.Client.UserInterface.Systems.Chat;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Robust.Client.Replays.UI;
using static Robust.Client.UserInterface.Controls.LayoutContainer;

namespace Content.Client.Replay.UI;

/// <summary>
/// Gameplay state when observing/spectating an entity during a replay.
/// </summary>
[Virtual]
public class ReplaySpectateEntityState : GameplayState
{
    protected override void Startup()
    {
        base.Startup();

        var screen = UserInterfaceManager.ActiveScreen;
        if (screen == null)
            return;

        screen.ShowWidget<GameTopMenuBar>(false);
        SetAnchorAndMarginPreset(screen.GetOrAddWidget<ReplayControlWidget>(), LayoutPreset.TopLeft, margin: 10);

        foreach (var chatbox in UserInterfaceManager.GetUIController<ChatUIController>().Chats)
        {
            chatbox.ChatInput.Visible = false;
        }
    }

    protected override void Shutdown()
    {
        var screen = UserInterfaceManager.ActiveScreen;
        if (screen != null)
        {
            screen.RemoveWidget<ReplayControlWidget>();
            screen.ShowWidget<GameTopMenuBar>(true);
        }

        foreach (var chatbox in UserInterfaceManager.GetUIController<ChatUIController>().Chats)
        {
            chatbox.ChatInput.Visible = true;
        }

        base.Shutdown();
    }
}
