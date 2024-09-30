using Content.Client.Administration.Managers;
using Content.Client.Eui;
using Content.Shared.Administration;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Client.Console;
using Robust.Client.UserInterface;

namespace Content.Client.Administration.UI.PlayerPanel;

[UsedImplicitly]
public sealed class PlayerPanelEui : BaseEui
{
    [Dependency] private readonly IClientConsoleHost _console = default!;
    [Dependency] private readonly IClientAdminManager _admin = default!;
    [Dependency] private readonly IClipboardManager _clipboard = default!;

    private PlayerPanel PlayerPanel { get;  }

    public PlayerPanelEui()
    {
        PlayerPanel = new PlayerPanel(_admin);

        PlayerPanel.OnUsernameCopy += username => _clipboard.SetText(username);
        PlayerPanel.OnOpenNotes += id => _console.ExecuteCommand($"adminnotes \"{id}\"");
        // Kick command does not support GUIDs
        PlayerPanel.OnKick += username => _console.ExecuteCommand($"kick \"{username}\"");
        PlayerPanel.OnOpenBanPanel += id => _console.ExecuteCommand($"banpanel \"{id}\"");
        PlayerPanel.OnOpenBans += id => _console.ExecuteCommand($"banlist \"{id}\"");
        PlayerPanel.OnAhelp += id => _console.ExecuteCommand($"openahelp \"{id}\"");
        PlayerPanel.OnWhitelistToggle += (id, whitelisted) =>
        {
            _console.ExecuteCommand(whitelisted ? $"whitelistremove \"{id}\"" : $"whitelistadd \"{id}\"");
        };

        PlayerPanel.OnFreezeAndMuteToggle += () => SendMessage(new PlayerPanelFreezeMessage(true));
        PlayerPanel.OnFreeze += () => SendMessage(new PlayerPanelFreezeMessage());
        PlayerPanel.OnLogs += () => SendMessage(new PlayerPanelLogsMessage());
        PlayerPanel.OnRejuvenate += () => SendMessage(new PlayerPanelRejuvenationMessage());
        PlayerPanel.OnDelete+= () => SendMessage(new PlayerPanelDeleteMessage());

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
        PlayerPanel.SetUsername(s.Username);
        PlayerPanel.SetPlaytime(s.Playtime);
        PlayerPanel.SetBans(s.TotalBans, s.TotalRoleBans);
        PlayerPanel.SetNotes(s.TotalNotes);
        PlayerPanel.SetWhitelisted(s.Whitelisted);
        PlayerPanel.SetSharedConnections(s.SharedConnections);
        PlayerPanel.SetFrozen(s.CanFreeze, s.Frozen);
        PlayerPanel.SetAhelp(s.CanAhelp);
        PlayerPanel.SetButtons();
    }
}
