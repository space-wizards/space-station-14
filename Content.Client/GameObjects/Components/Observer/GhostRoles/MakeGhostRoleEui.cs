using Content.Client.Eui;
using Content.Shared.Eui;
using Content.Shared.GameObjects.Components.Observer.GhostRoles;
using JetBrains.Annotations;
using Robust.Client.Console;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Content.Client.GameObjects.Components.Observer.GhostRoles
{
    [UsedImplicitly]
    public class MakeGhostRoleEui : BaseEui
    {
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

            _window.SetEntity(uiState.EntityUid);
        }

        public override void Opened()
        {
            base.Opened();
            _window.OpenCentered();
        }

        private void OnMake(EntityUid uid, string name, string description)
        {
            var player = _playerManager.LocalPlayer;
            if (player == null)
            {
                return;
            }

            var command =
                $"makeghostrole " +
                $"\"{CommandParsing.Escape(uid.ToString())}\" " +
                $"\"{CommandParsing.Escape(name)}\" " +
                $"\"{CommandParsing.Escape(description)}\"";

            _consoleHost.ExecuteCommand(player.Session, command);
            _window.Close();
        }

        private void OnClose()
        {
            base.Closed();
            SendMessage(new MakeGhostRoleWindowClosedMessage());
        }
    }
}
