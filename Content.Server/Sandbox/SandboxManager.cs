using Content.Server.GameObjects;
using Content.Server.GameObjects.Components;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameTicking;
using Content.Server.Interfaces.GameTicking;
using Content.Shared.Sandbox;
using Robust.Server.Interfaces.Console;
using Robust.Server.Console;
using Robust.Server.Interfaces.Placement;
using Robust.Server.Interfaces.Player;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Content.Server.Sandbox
{
    internal sealed class SandboxManager : SharedSandboxManager, ISandboxManager
    {
#pragma warning disable 649
        [Dependency] private readonly IPlayerManager _playerManager;
        [Dependency] private readonly IServerNetManager _netManager;
        [Dependency] private readonly IGameTicker _gameTicker;
        [Dependency] private readonly IPlacementManager _placementManager;
        [Dependency] private readonly IConGroupController _conGroupController;
        [Dependency] private readonly IEntityManager _entityManager;
        [Dependency] private readonly IConsoleShell _shell;
#pragma warning restore 649

        private bool _isSandboxEnabled;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool IsSandboxEnabled
        {
            get => _isSandboxEnabled;
            set
            {
                _isSandboxEnabled = value;
                UpdateSandboxStatusForAll();
            }
        }

        public void Initialize()
        {
            _netManager.RegisterNetMessage<MsgSandboxStatus>(nameof(MsgSandboxStatus));
            _netManager.RegisterNetMessage<MsgSandboxRespawn>(nameof(MsgSandboxRespawn), SandboxRespawnReceived);
            _netManager.RegisterNetMessage<MsgSandboxGiveAccess>(nameof(MsgSandboxGiveAccess), SandboxGiveAccessReceived);
            _netManager.RegisterNetMessage<MsgSandboxGiveAghost>(nameof(MsgSandboxGiveAghost), SandboxGiveAghostReceived);

            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
            _gameTicker.OnRunLevelChanged += GameTickerOnOnRunLevelChanged;

            _placementManager.AllowPlacementFunc = placement =>
            {
                if (IsSandboxEnabled)
                {
                    return true;
                }

                var channel = placement.MsgChannel;
                var player = _playerManager.GetSessionByChannel(channel);

                if (_conGroupController.CanAdminPlace(player))
                {
                    return true;
                }

                return false;
            };
        }

        private void GameTickerOnOnRunLevelChanged(GameRunLevelChangedEventArgs obj)
        {
            // Automatically clear sandbox state when round resets.
            if (obj.NewRunLevel == GameRunLevel.PreRoundLobby)
            {
                IsSandboxEnabled = false;
            }
        }

        private void OnPlayerStatusChanged(object sender, SessionStatusEventArgs e)
        {
            if (e.NewStatus != SessionStatus.Connected || e.OldStatus != SessionStatus.Connecting)
            {
                return;
            }

            var msg = _netManager.CreateNetMessage<MsgSandboxStatus>();
            msg.SandboxAllowed = IsSandboxEnabled;
            _netManager.ServerSendMessage(msg, e.Session.ConnectedClient);
        }

        private void SandboxRespawnReceived(MsgSandboxRespawn message)
        {
            if (!IsSandboxEnabled)
            {
                return;
            }

            var player = _playerManager.GetSessionByChannel(message.MsgChannel);
            _gameTicker.Respawn(player);
        }

        private void SandboxGiveAccessReceived(MsgSandboxGiveAccess message)
        {
            if(!IsSandboxEnabled)
            {
                return;
            }

            var player = _playerManager.GetSessionByChannel(message.MsgChannel);
            if(player.AttachedEntity.TryGetComponent<HandsComponent>(out var hands))
            {
                ;
                hands.PutInHandOrDrop(
                    _entityManager.SpawnEntity("CaptainIDCard",
                    player.AttachedEntity.Transform.GridPosition).GetComponent<ItemComponent>());
            }
        }

        private void SandboxGiveAghostReceived(MsgSandboxGiveAghost message)
        {
            if (!IsSandboxEnabled)
            {
                return;
            }

            var player = _playerManager.GetSessionByChannel(message.MsgChannel);
            _shell.ExecuteCommand(player, $"suicide");
        }

        private void UpdateSandboxStatusForAll()
        {
            var msg = _netManager.CreateNetMessage<MsgSandboxStatus>();
            msg.SandboxAllowed = IsSandboxEnabled;
            _netManager.ServerSendToAll(msg);
        }
    }
}
