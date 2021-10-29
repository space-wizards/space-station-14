using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Administration.Events;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Players;

namespace Content.Client.Administration
{
    public partial class AdminSystem : EntitySystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public event Action<List<PlayerListChangedEvent.PlayerInfo>>? PlayerListChanged;

        private List<PlayerListChangedEvent.PlayerInfo>? _playerList;
        public IReadOnlyList<PlayerListChangedEvent.PlayerInfo> PlayerList
        {
            get
            {
                if (_playerList != null) return _playerList;

                return new List<PlayerListChangedEvent.PlayerInfo>();
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            InitializeOverlay();
            InitializeMenu();
            SubscribeNetworkEvent<PlayerListChangedEvent>(OnPlayerListChanged);
        }

        private void OnPlayerListChanged(PlayerListChangedEvent msg, EntitySessionEventArgs args)
        {
            _playerList = msg.PlayersInfo;
            UpdateOverlay(msg.PlayersInfo);
            PlayerListChanged?.Invoke(msg.PlayersInfo);
        }
    }
}
