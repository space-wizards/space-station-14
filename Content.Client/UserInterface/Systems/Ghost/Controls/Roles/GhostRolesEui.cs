using System.Linq;
using Content.Client.Eui;
using Content.Shared.Eui;
using Content.Shared.Ghost.Roles;
using JetBrains.Annotations;

namespace Content.Client.UserInterface.Systems.Ghost.Controls.Roles
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

            _window.OnRoleRequested += info =>
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

            _window.OnRoleFollow += info =>
            {
                SendMessage(new GhostRoleFollowRequestMessage(info.Identifier));
            };

            _window.OnClose += () =>
            {
                SendMessage(new CloseEuiMessage());
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
            _window.ClearEntries();

            var groupedRoles = ghostState.GhostRoles.GroupBy(
                role => (role.Name, role.Description));
            foreach (var group in groupedRoles)
            {
                var name = group.Key.Name;
                var description = group.Key.Description;

                _window.AddEntry(name, description, group);
            }

            var closeRulesWindow = ghostState.GhostRoles.All(role => role.Identifier != _windowRulesId);
            if (closeRulesWindow)
            {
                _windowRules?.Close();
            }
        }
    }
}
