using System.Linq;
using Content.Shared.Administration;
using Content.Shared.Administration.Events;
using Content.Shared.GameTicking;
using Robust.Shared.Network;

namespace Content.Client.Administration.Systems
{
    public sealed partial class AdminSystem : EntitySystem
    {
        public event Action<List<PlayerInfo>>? PlayerListChanged;

        public event Action<List<GameRuleInfo>>? GameRulesListChanged;

        private Dictionary<NetUserId, PlayerInfo>? _playerList;
        public IReadOnlyList<PlayerInfo> PlayerList
        {
            get
            {
                if (_playerList != null) return _playerList.Values.ToList();

                return new List<PlayerInfo>();
            }
        }

        private List<GameRuleInfo>? _gameRulesList;
        public IReadOnlyList<GameRuleInfo> GameRulesList
        {
            get
            {
                if (_gameRulesList != null) return _gameRulesList;

                return new List<GameRuleInfo>();
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            InitializeOverlay();
            SubscribeNetworkEvent<FullPlayerListEvent>(OnPlayerListChanged);
            SubscribeNetworkEvent<PlayerInfoChangedEvent>(OnPlayerInfoChanged);
            SubscribeNetworkEvent<GameRulesListEvent>(OnGameRulesListChanged);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            ShutdownOverlay();
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

        private void OnGameRulesListChanged(GameRulesListEvent msg)
        {
            _gameRulesList = msg.ActiveGameRules;
            GameRulesListChanged?.Invoke(msg.ActiveGameRules);
        }
    }
}
