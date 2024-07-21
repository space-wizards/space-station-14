using Content.Client.Administration.Managers;
using Content.Client.Eui;
using Content.Client.Info.PlaytimeStats;
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
    [Dependency] private readonly IClientAdminManager _adminManager = default!;

    private PlayerPanel PlayerPanel { get;  }

    public PlayerPanelEui()
    {
        PlayerPanel = new PlayerPanel(_adminManager);
        PlayerPanel.OnOpenNotes += id => _console.ExecuteCommand($"adminnotes {id}");
        // Kick command does not support GUIDs
        PlayerPanel.OnKick += username => _console.ExecuteCommand($"kick {username}");
        PlayerPanel.OnOpenBanPanel += id => _console.ExecuteCommand($"banpanel {id}");
        PlayerPanel.OnOpenBans += id => _console.ExecuteCommand($"banlist {id}");
        PlayerPanel.OnOpenNotes += id => _console.ExecuteCommand($"adminnotes {id}");
        PlayerPanel.OnWhitelistToggle += (id, whitelisted) =>
        {
            if (whitelisted)
                _console.ExecuteCommand($"whitelistremove {id}");
            else
            {
                _console.ExecuteCommand($"whitelistadd {id}");
            }
        };

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
        PlayerPanel.SetButtons();
    }
}
