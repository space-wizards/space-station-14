using Content.Client.Eui;
using Content.Shared.Eui;
using Content.Shared.Ghost.Roles;
using JetBrains.Annotations;
using Robust.Client.Console;
using Robust.Client.Player;
using Robust.Shared.Utility;

namespace Content.Client.Ghost.Roles.UI;

[UsedImplicitly]
public class GhostRoleGroupsEui : BaseEui
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IClientConsoleHost _consoleHost = default!;

    private readonly AdminGhostRoleGroupsWindow _window;
    private GhostRoleGroupStartWindow? _windowStartRoleGroup;
    private GhostRoleGroupDeleteWindow? _windowDeleteRoleGroup;

    public GhostRoleGroupsEUI()
    {
        _window = new AdminGhostRoleGroupsWindow();

        _window.OnGroupStart += () =>
        {
            _windowStartRoleGroup?.Close();
            _windowStartRoleGroup = new GhostRoleGroupStartWindow(OnGroupStart);
            _windowStartRoleGroup.OpenCentered();
        };

        _window.OnGroupDelete += info =>
        {
            _windowDeleteRoleGroup?.Close();
            _windowDeleteRoleGroup = new GhostRoleGroupDeleteWindow(info.GroupIdentifier, OnGroupDelete);
            _windowDeleteRoleGroup.OpenCentered();
        };

        _window.OnGroupRelease += info =>
        {
            OnGroupRelease(info.GroupIdentifier);
        };
    }

    private void OnGroupStart(string name, string description, string rules)
    {
        var player = _playerManager.LocalPlayer;
        if (player == null)
        {
            return;
        }

        var startGhostRoleGroupCommand =
            $"ghostrolegroups start " +
            $"\"{CommandParsing.Escape(name)}\"" +
            $"\"{CommandParsing.Escape(description)}\"" +
            $"\"{CommandParsing.Escape(rules)}\"";

        _consoleHost.ExecuteCommand(player.Session, startGhostRoleGroupCommand);
        _windowStartRoleGroup?.Close();
    }

    private void OnGroupDelete(uint identifier, bool deleteEntities)
    {
        var player = _playerManager.LocalPlayer;
        if (player == null)
            return;

        var deleteGhostRoleGroupCommand =
            $"ghostrolegroups delete " +
            $"\"{CommandParsing.Escape(deleteEntities.ToString())}\"" +
            $"\"{CommandParsing.Escape(identifier.ToString())}\"";

        _consoleHost.ExecuteCommand(player.Session, deleteGhostRoleGroupCommand);
        _windowDeleteRoleGroup?.Close();
    }

    private void OnGroupRelease(uint identifier)
    {
        var player = _playerManager.LocalPlayer;
        if (player == null)
            return;

        var releaseGhostRoleGroupCommand =
            $"ghostrolegroups release " +
            $"\"{CommandParsing.Escape(identifier.ToString())}\"";

        _consoleHost.ExecuteCommand(player.Session, releaseGhostRoleGroupCommand);
    }

    public override void Opened()
    {
        base.Opened();
        _window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();
        _window.Close();
        _windowDeleteRoleGroup?.Close();
        _windowStartRoleGroup?.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        base.HandleState(state);

        if (state is not AdminGhostRolesEuiState groupState)
            return;

        _window.ClearEntries();
        foreach (var group in groupState.AdminGhostRoleGroups)
        {
            _window.AddEntry(group);
        }
    }


}
