using Content.Client.UserInterface.Systems.Actions.Widgets;
using Content.Client.UserInterface.Systems.Alerts.Widgets;
using Content.Client.UserInterface.Systems.Ghost.Widgets;
using Content.Client.UserInterface.Systems.Hotbar.Widgets;

namespace Content.Client.Replay.UI;

/// <summary>
/// Gameplay state when moving around a replay as a ghost.
/// </summary>
public sealed class ReplayGhostState : ReplaySpectateEntityState
{
    protected override void Startup()
    {
        base.Startup();

        var screen = UserInterfaceManager.ActiveScreen;
        if (screen == null)
            return;

        screen.ShowWidget<GhostGui>(false);
        screen.ShowWidget<ActionsBar>(false);
        screen.ShowWidget<AlertsUI>(false);
        screen.ShowWidget<HotbarGui>(false);
    }

    protected override void Shutdown()
    {
        var screen = UserInterfaceManager.ActiveScreen;
        if (screen != null)
        {
            screen.ShowWidget<GhostGui>(true);
            screen.ShowWidget<ActionsBar>(true);
            screen.ShowWidget<AlertsUI>(true);
            screen.ShowWidget<HotbarGui>(true);
        }

        base.Shutdown();
    }
}
