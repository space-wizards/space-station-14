using Content.Client.Eui;
using Content.Shared.Administration;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Client.Console;

namespace Content.Client.Administration.UI.PlayerPanel;

[UsedImplicitly]
public sealed class PlayerPanelEui : BaseEui
{
    [Dependency] private readonly EuiManager _eui = default!;
    [Dependency] private readonly IClientConsoleHost _console = default!;

    private PlayerPanel PlayerPanel { get;  }

    public PlayerPanelEui()
    {
        PlayerPanel = new PlayerPanel();
        PlayerPanel.OnClose += () => SendMessage(new CloseEuiMessage());
    }

    public override void Opened()
    {
        PlayerPanel.OpenCentered();
    }

    public override void Closed()
    {
        PlayerPanel.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not PlayerPanelEuiState s)
            return;

        PlayerPanel.TargetPlayer = s.Guid;
        PlayerPanel.TargetUsername = s.Username;
        PlayerPanel.SetTitle(s.Username);
        PlayerPanel.SetPlaytime(s.Playtime);
        PlayerPanel.SetBans(s.TotalBans, s.TotalRoleBans);
        PlayerPanel.SetNotes(s.TotalNotes);
        PlayerPanel.SetWhitelisted(s.Whitelisted);
    }

}
