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
        private GhostRoleRulesWindow? _windowRules = null;
        private uint _windowRulesId = 0;

        public GhostRolesEui()
        {
            _window = new GhostRolesWindow();

            _window.RoleRequested += info =>
            {
                if (_windowRules != null)
                    _windowRules.Close();
                _windowRules = new GhostRoleRulesWindow(info.Rules, _ =>
                {
                    SendMessage(new GhostRoleTakeoverRequestMessage(info.Identifier));
                });
                _windowRulesId = info.Identifier;
                _windowRules.OnClose += () =>
                {
                    _windowRules = null;
                };
                _windowRules.OpenCentered();
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

            if (state is not GhostRolesEuiState ghostState) return;

            var closeRulesWindow = true;

            _window.ClearEntries();

            foreach (var info in ghostState.GhostRoles)
            {
                _window.AddEntry(info);
                if (info.Identifier == _windowRulesId)
                {
                    closeRulesWindow = false;
                }
            }

            if (closeRulesWindow)
            {
                _windowRules?.Close();
            }
        }
    }
}
