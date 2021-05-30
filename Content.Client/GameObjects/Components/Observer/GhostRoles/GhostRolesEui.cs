using Content.Client.Eui;
using Content.Shared.Eui;
using Content.Shared.GameObjects.Components.Observer;
using Content.Shared.GameObjects.Components.Observer.GhostRoles;
using JetBrains.Annotations;

namespace Content.Client.GameObjects.Components.Observer.GhostRoles
{
    [UsedImplicitly]
    public class GhostRolesEui : BaseEui
    {
        private readonly GhostRolesWindow _window;

        public GhostRolesEui()
        {
            _window = new GhostRolesWindow();

            _window.RoleRequested += id =>
            {
                SendMessage(new GhostRoleTakeoverRequestMessage(id));
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
        }

        public override void HandleState(EuiStateBase state)
        {
            base.HandleState(state);

            if (state is not GhostRolesEuiState ghostState) return;

            _window.ClearEntries();

            foreach (var info in ghostState.GhostRoles)
            {
                _window.AddEntry(info);
            }
        }
    }
}
