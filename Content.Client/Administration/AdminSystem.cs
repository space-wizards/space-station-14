using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.Administration.UI.Tabs;
using Content.Shared.Administration;
using Content.Shared.Administration.Events;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Content.Client.Administration
{
    public partial class AdminSystem : EntitySystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public event Action<List<PlayerInfo>>? PlayerListChanged;
        public event Action<List<string>>? LogsChanged;

        private Dictionary<NetUserId, PlayerInfo>? _playerList;
        public IReadOnlyList<PlayerInfo> PlayerList
        {
            get
            {
                if (_playerList != null) return _playerList.Values.ToList();

                return new List<PlayerInfo>();
            }
        }

        public List<string> Logs { get; set; } = new();

        public override void Initialize()
        {
            base.Initialize();

            InitializeOverlay();
            InitializeMenu();
            SubscribeNetworkEvent<FullPlayerListEvent>(OnPlayerListChanged);
            SubscribeNetworkEvent<PlayerInfoChangedEvent>(OnPlayerInfoChanged);
            SubscribeNetworkEvent<PlayerInfoRemovalMessage>(OnPlayerInfoRemoval);
            SubscribeNetworkEvent<LogsMessage>(OnLogsReceived);
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

        private void OnLogsReceived(LogsMessage ev)
        {
            Logs = ev.Logs;
            LogsChanged?.Invoke(Logs);
        }

        public void TabChanged(Control control)
        {
            if (control is LogsTab)
            {
                RaiseNetworkEvent(new RequestLogsMessage());
            }
        }
    }
}
