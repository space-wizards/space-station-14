using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Administration;
using Content.Shared.Administration.Events;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Players;

namespace Content.Client.Administration
{
    public partial class AdminSystem : EntitySystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public event Action<List<PlayerInfo>>? PlayerListChanged;

        private Dictionary<NetUserId, PlayerInfo>? _playerList;
        public IReadOnlyList<PlayerInfo> PlayerList
        {
            get
            {
                if (_playerList != null) return _playerList.Values.ToList();

                return new List<PlayerInfo>();
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            InitializeOverlay();
            InitializeMenu();
            SubscribeNetworkEvent<FullPlayerListEvent>(OnPlayerListChanged);
            SubscribeNetworkEvent<PlayerInfoChangedEvent>(OnPlayerInfoChanged);
            SubscribeNetworkEvent<PlayerInfoRemovalMessage>(OnPlayerInfoRemoval);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            ShutdownOverlay();
        }

        private void OnPlayerInfoRemoval(PlayerInfoRemovalMessage ev)
        {
            if (_playerList == null) _playerList = new();

            _playerList.Remove(ev.NetUserId);
            PlayerListChanged?.Invoke(_playerList.Values.ToList());
        }

        private void OnPlayerInfoChanged(PlayerInfoChangedEvent ev)
        {
            if(ev.PlayerInfo == null) return;

            if (_playerList == null) _playerList = new();

            _playerList[ev.PlayerInfo.SessionId] = ev.PlayerInfo;
            PlayerListChanged?.Invoke(_playerList.Values.ToList());
        }

        private void OnPlayerListChanged(FullPlayerListEvent msg)
        {
            _playerList = msg.PlayersInfo.ToDictionary(x => x.SessionId, x => x);
            PlayerListChanged?.Invoke(msg.PlayersInfo);
        }
    }
}
