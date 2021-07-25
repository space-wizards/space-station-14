using System;
using System.Linq;
using Content.Shared.GameTicking;
using Content.Shared.Suspicion;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;


namespace Content.Server.Suspicion.EntitySystems
{
    [UsedImplicitly]
    public sealed class SuspicionEndTimerSystem : EntitySystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = null!;

        private TimeSpan? _endTime;

        public TimeSpan? EndTime
        {
            get => _endTime;
            set
            {
                _endTime = value;
                SendUpdateToAll();
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);

            _playerManager.PlayerStatusChanged += PlayerManagerOnPlayerStatusChanged;
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _playerManager.PlayerStatusChanged -= PlayerManagerOnPlayerStatusChanged;
        }

        private void PlayerManagerOnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            if (e.NewStatus == SessionStatus.InGame)
            {
                SendUpdateTimerMessage(e.Session);
            }
        }

        private void SendUpdateToAll()
        {
            foreach (var player in _playerManager.GetAllPlayers().Where(p => p.Status == SessionStatus.InGame))
            {
                SendUpdateTimerMessage(player);
            }
        }

        private void SendUpdateTimerMessage(IPlayerSession player)
        {
            var msg = new SuspicionMessages.SetSuspicionEndTimerMessage
            {
                EndTime = EndTime
            };

            EntityManager.EntityNetManager?.SendSystemNetworkMessage(msg, player.ConnectedClient);
        }

        private void Reset(RoundRestartCleanupEvent ev)
        {
            EndTime = null;
        }
    }
}
