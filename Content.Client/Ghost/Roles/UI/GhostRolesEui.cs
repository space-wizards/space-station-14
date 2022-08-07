using System.Linq;
using Content.Client.Eui;
using Content.Shared.Eui;
using Content.Shared.Ghost.Roles;
using JetBrains.Annotations;
using Robust.Client.Console;
using Robust.Client.Player;
using Robust.Shared.Utility;

namespace Content.Client.Ghost.Roles.UI
{
    [UsedImplicitly]
    public sealed class GhostRolesEui : BaseEui
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IClientConsoleHost _consoleHost = default!;

        private readonly GhostRolesWindow _window;
        private GhostRoleRulesWindow? _windowRules;
        private GhostRoleGroupStartWindow? _windowStartRoleGroup;
        private GhostRoleGroupDeleteWindow? _windowDeleteRoleGroup;
        private string _windowRulesId = "";

        public GhostRolesEui()
        {
            _window = new GhostRolesWindow();
            IoCManager.InjectDependencies(_window);

            _window.OnRoleTake += info =>
            {
                _windowRules?.Close();
                _windowRules = new GhostRoleRulesWindow(info.Rules, _ =>
                {
                    SendMessage(new GhostRoleTakeoverRequestMessage(info.Identifier));
                    _windowRules?.Close();
                });
                _windowRulesId = info.Name;
                _windowRules.OnClose += () =>
                {
                    _windowRules = null;
                };
                _windowRules.OpenCentered();
            };

            _window.OnRoleRequested += info =>
            {
                _windowRules?.Close();
                _windowRules = new GhostRoleRulesWindow(info.Rules, _ =>
                {
                    SendMessage(new GhostRoleLotteryRequestMessage(info.Identifier));
                    _windowRules?.Close();
                });
                _windowRulesId = info.Name;
                _windowRules.OnClose += () =>
                {
                    _windowRules = null;
                };
                _windowRules.OpenCentered();
            };

            _window.OnRoleCancelled += info =>
            {
               SendMessage(new GhostRoleCancelLotteryRequestMessage(info.Identifier));
            };

            _window.OnRoleFollowed += info =>
            {
                SendMessage(new GhostRoleFollowRequestMessage(info.Identifier));
            };

            _window.OnGroupRequested += info =>
            {
                SendMessage(new GhostRoleGroupLotteryRequestMessage(info.GroupIdentifier));
            };

            _window.OnGroupCancelled += info =>
            {
                SendMessage(new GhostRoleGroupCancelLotteryMessage(info.GroupIdentifier));
            };

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

            _window.OnClose += () =>
            {
                SendMessage(new GhostRoleWindowCloseMessage());
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
            _windowRules?.Close();
            _windowDeleteRoleGroup?.Close();
            _windowStartRoleGroup?.Close();
        }

        public override void HandleState(EuiStateBase state)
        {
            base.HandleState(state);

            if (state is not GhostRolesEuiState ghostState)
                return;

            _window.SetAdminControlsVisible(ghostState.ShowAdminControls);
            _window.SetLotteryTime(ghostState.LotteryStart, ghostState.LotteryEnd);
            _window.ClearEntries();

            foreach (var group in ghostState.GhostRoleGroups)
            {
                _window.AddGroupEntry(group, ghostState.ShowAdminControls);
            }

            foreach (var role in ghostState.GhostRoles)
            {
                _window.AddEntry(role);
            }

            var closeRulesWindow = ghostState.GhostRoles.All(role => role.Identifier != _windowRulesId);
            if (closeRulesWindow)
            {
                _windowRules?.Close();
            }
        }
    }
}
