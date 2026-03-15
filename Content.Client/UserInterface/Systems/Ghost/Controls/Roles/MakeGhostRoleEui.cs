using Content.Client.Eui;
using Content.Shared.Eui;
using Content.Shared.Ghost.Roles;
using Content.Shared.Ghost.Roles.Raffles;
using JetBrains.Annotations;
using Robust.Client.Console;
using Robust.Client.Player;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.Ghost.Controls.Roles;

[UsedImplicitly]
public sealed class MakeGhostRoleEui : BaseEui
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IClientConsoleHost _consoleHost = default!;

    private readonly MakeGhostRoleWindow _window;

    public MakeGhostRoleEui()
    {
        _window = new MakeGhostRoleWindow();

        _window.OnClose += OnClose;
        _window.OnMake += OnMake;
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not MakeGhostRoleEuiState uiState)
        {
            return;
        }

        _window.SetEntity(_entManager, uiState.Entity);
    }

    public override void Opened()
    {
        base.Opened();
        _window.OpenCentered();
    }

    private void OnMake(NetEntity entity, string name, string description, string rules, bool makeSentient, GhostRoleRaffleSettings? raffleSettings)
    {
        var session = _playerManager.LocalSession;
        if (session == null)
        {
            return;
        }

        var command = raffleSettings is not null ? "makeghostroleraffled" : "makeghostrole";

        var makeGhostRoleCommand =
            $"{command} " +
            $"\"{CommandParsing.Escape(entity.ToString())}\" " +
            $"\"{CommandParsing.Escape(name)}\" " +
            $"\"{CommandParsing.Escape(description)}\" ";

        if (raffleSettings is not null)
        {
            makeGhostRoleCommand += $"{raffleSettings.InitialDuration} " +
                                    $"{raffleSettings.JoinExtendsDurationBy} " +
                                    $"{raffleSettings.MaxDuration} ";
        }

        makeGhostRoleCommand += $"\"{CommandParsing.Escape(rules)}\"";

        _consoleHost.ExecuteCommand(session, makeGhostRoleCommand);

        if (makeSentient)
        {
            var makeSentientCommand = $"makesentient \"{CommandParsing.Escape(entity.ToString())}\"";
            _consoleHost.ExecuteCommand(session, makeSentientCommand);
        }

        _window.Close();
    }

    private void OnClose()
    {
        base.Closed();
        SendMessage(new CloseEuiMessage());
    }
}
