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

        private string _windowRulesId = "";

        public GhostRolesEui()
        {
            _window = new GhostRolesWindow();

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
                _windowRules?.Close();
                _windowRules = new GhostRoleRulesWindow(Loc.GetString("ghost-role-component-default-rules"), _ =>
                {
                    SendMessage(new GhostRoleGroupLotteryRequestMessage(info.Identifier));
                    _windowRules?.Close();
                });
                _windowRulesId = info.Name;
                _windowRules.OnClose += () =>
                {
                    _windowRules = null;
                };
                _windowRules.OpenCentered();
            };

            _window.OnGroupCancelled += info =>
            {
                SendMessage(new GhostRoleGroupCancelLotteryMessage(info.Identifier));
            };

            _window.OnRoleGroupsOpened += OnRoleGroupsOpen;

            _window.OnClose += () =>
            {
                SendMessage(new GhostRoleWindowCloseMessage());
            };
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
        }

        private void OnRoleGroupsOpen()
        {
            var player = _playerManager.LocalPlayer;
            if (player == null)
                return;

            _consoleHost.ExecuteCommand(player.Session, "ghostrolegroups open");
        }

        public override void HandleState(EuiStateBase state)
        {
            base.HandleState(state);

            if (state is not GhostRolesEuiState ghostState)
                return;

            _window.SetLotteryTime(ghostState.LotteryStart, ghostState.LotteryEnd);
            _window.ClearEntries();

            foreach (var group in ghostState.GhostRoleGroups)
            {
                _window.AddGroupEntry(group, ghostState.PlayerRoleGroupRequests.Contains(group.Identifier));
            }

            foreach (var role in ghostState.GhostRoles)
            {
                _window.AddEntry(role, ghostState.PlayerGhostRoleRequests.Contains(role.Identifier));
            }

            var closeRulesWindow = ghostState.GhostRoles.All(role => role.Name != _windowRulesId);
            if (closeRulesWindow)
            {
                _windowRules?.Close();
            }
        }
    }
}
