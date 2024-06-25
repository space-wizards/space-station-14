using Content.Client.Administration.UI.Notes;
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

        PlayerPanel.OnOpenNotes += player =>
        { };

        PlayerPanel.OnOpenBans += player =>
        { };

        PlayerPanel.OnOpenAhelp += player =>
        { };

        PlayerPanel.OnFreezeAndMute += player =>
        { };

        PlayerPanel.OnUnfreeze += player =>
        { };

        PlayerPanel.OnKick+= player =>
        { };

        PlayerPanel.OnOpenBanPanel += player =>
        { };
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not PlayerPanelEuiState s)
            return;
    }

}
