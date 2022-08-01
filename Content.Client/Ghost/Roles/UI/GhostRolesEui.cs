using System.Linq;
using Content.Client.Eui;
using Content.Shared.Eui;
using Content.Shared.Ghost.Roles;
using JetBrains.Annotations;

namespace Content.Client.Ghost.Roles.UI
{
    [UsedImplicitly]
    public sealed class GhostRolesEui : BaseEui
    {
        private readonly GhostRolesWindow _window;
        private GhostRoleRulesWindow? _windowRules;
        private string _windowRulesId = "";

        public GhostRolesEui()
        {
            _window = new GhostRolesWindow();
            IoCManager.InjectDependencies(_window);

            _window.OnRoleRequested += info =>
            {
                if (_windowRules != null)
                    _windowRules.Close();
                _windowRules = new GhostRoleRulesWindow(info.Rules, _ =>
                {
                    SendMessage(new GhostRoleTakeoverRequestMessage(info.Name));
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
               SendMessage(new GhostRoleCancelTakeoverRequestMessage(info.Name));
            };

            _window.OnRoleFollowed += info =>
            {
                SendMessage(new GhostRoleFollowRequestMessage(info.Name));
            };

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

        public override void HandleState(EuiStateBase state)
        {
            base.HandleState(state);

            if (state is not GhostRolesEuiState ghostState)
                return;

            _window.SetLotteryTime(ghostState.LotteryStart, ghostState.LotteryEnd);
            _window.ClearEntries();

            foreach (var role in ghostState.GhostRoles)
            {
                _window.AddEntry(role);
            }

            var closeRulesWindow = ghostState.GhostRoles.All(role => role.Name != _windowRulesId);
            if (closeRulesWindow)
            {
                _windowRules?.Close();
            }
        }
    }
}
