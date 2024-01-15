using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Maps;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Voting;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;


namespace Content.Server.Voting.Managers
{
    public sealed partial class VoteManager : IVoteManager
    {
        [Dependency] private readonly IServerNetManager _netManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IAdminManager _adminMgr = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IGameMapManager _gameMapManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;

        private int _nextVoteId = 1;

        private readonly Dictionary<int, VoteReg> _votes = new();
        private readonly Dictionary<int, VoteHandle> _voteHandles = new();

        private readonly Dictionary<StandardVoteType, TimeSpan> _standardVoteTimeout = new();
        private readonly Dictionary<NetUserId, TimeSpan> _voteTimeout = new();
        private readonly HashSet<ICommonSession> _playerCanCallVoteDirty = new();
        private readonly StandardVoteType[] _standardVoteTypeValues = Enum.GetValues<StandardVoteType>();

        public void Initialize()
        {
            _netManager.RegisterNetMessage<MsgVoteData>();
            _netManager.RegisterNetMessage<MsgVoteCanCall>();
            _netManager.RegisterNetMessage<MsgVoteMenu>(ReceiveVoteMenu);

            _playerManager.PlayerStatusChanged += PlayerManagerOnPlayerStatusChanged;
            _adminMgr.OnPermsChanged += AdminPermsChanged;

            _cfg.OnValueChanged(CCVars.VoteEnabled, _ =>
            {
                DirtyCanCallVoteAll();
            });

            foreach (var kvp in _voteTypesToEnableCVars)
            {
                _cfg.OnValueChanged(kvp.Value, _ =>
                {
                    DirtyCanCallVoteAll();
                });
            }
        }

        private void ReceiveVoteMenu(MsgVoteMenu message)
        {
            var sender = message.MsgChannel;
            var session = _playerManager.GetSessionByChannel(sender);

            _adminLogger.Add(LogType.Vote, LogImpact.Low, $"{session} opened vote menu");
        }

        private void AdminPermsChanged(AdminPermsChangedEventArgs obj)
        {
            DirtyCanCallVote(obj.Player);
        }

        private void PlayerManagerOnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            if (e.NewStatus == SessionStatus.InGame)
            {
                // Send current votes to newly connected players.
                foreach (var voteReg in _votes.Values)
                {
                    SendSingleUpdate(voteReg, e.Session);
                }

                DirtyCanCallVote(e.Session);
            }
            else if (e.NewStatus == SessionStatus.Disconnected)
            {
                // Clear votes from disconnected players.
                foreach (var voteReg in _votes.Values)
                {
                    CastVote(voteReg, e.Session, null);
                }
            }
        }

        private void CastVote(VoteReg v, ICommonSession player, int? option)
        {
            if (!IsValidOption(v, option))
                throw new ArgumentOutOfRangeException(nameof(option), "Invalid vote option ID");

            if (v.CastVotes.TryGetValue(player, out var existingOption))
            {
                v.Entries[existingOption].Votes -= 1;
            }

            if (option != null)
            {
                v.Entries[option.Value].Votes += 1;
                v.CastVotes[player] = option.Value;
            }
            else
            {
                v.CastVotes.Remove(player);
            }

            v.VotesDirty.Add(player);
            v.Dirty = true;
        }

        private bool IsValidOption(VoteReg voteReg, int? option)
        {
            return option == null || option >= 0 && option < voteReg.Entries.Length;
        }

        public void Update()
        {
            // Handle active votes.
            var remQueue = new RemQueue<int>();
            foreach (var v in _votes.Values)
            {
                // Logger.Debug($"{_timing.ServerTime}");
                if (_timing.RealTime >= v.EndTime)
                    EndVote(v);

                if (v.Finished)
                    remQueue.Add(v.Id);

                if (v.Dirty)
                    SendUpdates(v);
            }

            foreach (var id in remQueue)
            {
                _votes.Remove(id);
                _voteHandles.Remove(id);
            }

            // Handle player timeouts.
            var timeoutRemQueue = new RemQueue<NetUserId>();
            foreach (var (userId, timeout) in _voteTimeout)
            {
                if (timeout < _timing.RealTime)
                    timeoutRemQueue.Add(userId);
            }

            foreach (var userId in timeoutRemQueue)
            {
                _voteTimeout.Remove(userId);

                if (_playerManager.TryGetSessionById(userId, out var session))
                    DirtyCanCallVote(session);
            }

            // Handle standard vote timeouts.
            var stdTimeoutRemQueue = new RemQueue<StandardVoteType>();
            foreach (var (type, timeout) in _standardVoteTimeout)
            {
                if (timeout < _timing.RealTime)
                    stdTimeoutRemQueue.Add(type);
            }

            foreach (var type in stdTimeoutRemQueue)
            {
                _standardVoteTimeout.Remove(type);

                DirtyCanCallVoteAll();
            }

            // Handle dirty canCallVotes.
            foreach (var dirtyPlayer in _playerCanCallVoteDirty)
            {
                if (dirtyPlayer.Status != SessionStatus.Disconnected)
                    SendUpdateCanCallVote(dirtyPlayer);
            }

            _playerCanCallVoteDirty.Clear();
        }

        public IVoteHandle CreateVote(VoteOptions options)
        {
            var id = _nextVoteId++;

            var entries = options.Options.Select(o => new VoteEntry(o.data, o.text)).ToArray();

            var start = _timing.RealTime;
            var end = start + options.Duration;
            var reg = new VoteReg(id, entries, options.Title, options.InitiatorText,
                options.InitiatorPlayer, start, end);

            var handle = new VoteHandle(this, reg);

            _votes.Add(id, reg);
            _voteHandles.Add(id, handle);

            if (options.InitiatorPlayer != null)
            {
                var timeout = options.InitiatorTimeout ?? options.Duration * 2;
                _voteTimeout[options.InitiatorPlayer.UserId] = _timing.RealTime + timeout;
            }

            DirtyCanCallVoteAll();

            return handle;
        }

        private void SendUpdates(VoteReg v)
        {
            foreach (var player in _playerManager.Sessions)
            {
                SendSingleUpdate(v, player);
            }

            v.VotesDirty.Clear();
            v.Dirty = false;
        }

        private void SendSingleUpdate(VoteReg v, ICommonSession player)
        {
            var msg = new MsgVoteData();

            msg.VoteId = v.Id;
            msg.VoteActive = !v.Finished;

            if (!v.Finished)
            {
                msg.VoteTitle = v.Title;
                msg.VoteInitiator = v.InitiatorText;
                msg.StartTime = v.StartTime;
                msg.EndTime = v.EndTime;
            }

            if (v.CastVotes.TryGetValue(player, out var cast))
            {
                // Only send info for your vote IF IT CHANGED.
                // Otherwise there would be a reconciliation b*g causing the UI to jump back and forth.
                // (votes are not in simulation so can't use normal prediction/reconciliation sadly).
                var dirty = v.VotesDirty.Contains(player);
                msg.IsYourVoteDirty = dirty;
                if (dirty)
                {
                    msg.YourVote = (byte) cast;
                }
            }

            msg.Options = new (ushort votes, string name)[v.Entries.Length];
            for (var i = 0; i < msg.Options.Length; i++)
            {
                ref var entry = ref v.Entries[i];
                msg.Options[i] = ((ushort) entry.Votes, entry.Text);
            }

            player.ConnectedClient.SendMessage(msg);
        }

        private void DirtyCanCallVoteAll()
        {
            _playerCanCallVoteDirty.UnionWith(_playerManager.Sessions);
        }

        private void SendUpdateCanCallVote(ICommonSession player)
        {
            var msg = new MsgVoteCanCall();
            msg.CanCall = CanCallVote(player, null, out var isAdmin, out var timeSpan);
            msg.WhenCanCallVote = timeSpan;

            if (isAdmin)
            {
                msg.VotesUnavailable = Array.Empty<(StandardVoteType, TimeSpan)>();
            }
            else
            {
                var votesUnavailable = new List<(StandardVoteType, TimeSpan)>();
                foreach (var v in _standardVoteTypeValues)
                {
                    if (CanCallVote(player, v, out _, out var typeTimeSpan))
                        continue;
                    votesUnavailable.Add((v, typeTimeSpan));
                }
                msg.VotesUnavailable = votesUnavailable.ToArray();
            }

            _netManager.ServerSendMessage(msg, player.ConnectedClient);
        }

        private bool CanCallVote(
            ICommonSession initiator,
            StandardVoteType? voteType,
            out bool isAdmin,
            out TimeSpan timeSpan)
        {
            isAdmin = false;
            timeSpan = default;

            // Admins can always call votes.
            if (_adminMgr.HasAdminFlag(initiator, AdminFlags.Admin))
            {
                isAdmin = true;
                return true;
            }

            // If voting is disabled, block votes.
            if (!_cfg.GetCVar(CCVars.VoteEnabled))
                return false;
            // Specific standard vote types can be disabled with cvars.
            if (voteType != null && _voteTypesToEnableCVars.TryGetValue(voteType.Value, out var cvar) && !_cfg.GetCVar(cvar))
                return false;

            // Cannot start vote if vote is already active (as non-admin).
            if (_votes.Count != 0)
                return false;

            // Standard vote on timeout, no calling.
            // Ghosts I understand you're dead but stop spamming the restart vote bloody hell.
            if (voteType != null && _standardVoteTimeout.TryGetValue(voteType.Value, out timeSpan))
                return false;

            // If only one Preset available thats not really a vote
            // Still allow vote if availbable one is different from current one
            if (voteType == StandardVoteType.Preset)
            {
                var presets = GetGamePresets();
                if (presets.Count == 1 && presets.Select(x => x.Key).Single() == _entityManager.System<GameTicker>().Preset?.ID)
                    return false;
            }

            return !_voteTimeout.TryGetValue(initiator.UserId, out timeSpan);
        }

        public bool CanCallVote(ICommonSession initiator, StandardVoteType? voteType = null)
        {
            return CanCallVote(initiator, voteType, out _, out _);
        }

        private void EndVote(VoteReg v)
        {
            if (v.Finished)
            {
                return;
            }

            // Find winner or stalemate.
            var winners = v.Entries
                .GroupBy(e => e.Votes)
                .OrderByDescending(g => g.Key)
                .First()
                .Select(e => e.Data)
                .ToImmutableArray();
            // Store all votes in order for webhooks
            var voteTally = new List<int>(); 
            foreach(var entry in v.Entries)
            {
                voteTally.Add(entry.Votes);
            }

            v.Finished = true;
            v.Dirty = true;
            var args = new VoteFinishedEventArgs(winners.Length == 1 ? winners[0] : null, winners, voteTally);
            v.OnFinished?.Invoke(_voteHandles[v.Id], args);
            DirtyCanCallVoteAll();
        }

        private void CancelVote(VoteReg v)
        {
            if (v.Cancelled)
                return;

            v.Cancelled = true;
            v.Finished = true;
            v.Dirty = true;
            v.OnCancelled?.Invoke(_voteHandles[v.Id]);
            DirtyCanCallVoteAll();
        }

        public IEnumerable<IVoteHandle> ActiveVotes => _voteHandles.Values;

        public bool TryGetVote(int voteId, [NotNullWhen(true)] out IVoteHandle? vote)
        {
            if (_voteHandles.TryGetValue(voteId, out var vHandle))
            {
                vote = vHandle;
                return true;
            }

            vote = default;
            return false;
        }

        private void DirtyCanCallVote(ICommonSession player)
        {
            _playerCanCallVoteDirty.Add(player);
        }

        #region Preset Votes

        private void WirePresetVoteInitiator(VoteOptions options, ICommonSession? player)
        {
            if (player != null)
            {
                options.SetInitiator(player);
            }
            else
            {
                options.InitiatorText = Loc.GetString("ui-vote-initiator-server");
            }
        }

        #endregion

        #region Vote Data

        private sealed class VoteReg
        {
            public readonly int Id;
            public readonly Dictionary<ICommonSession, int> CastVotes = new();
            public readonly VoteEntry[] Entries;
            public readonly string Title;
            public readonly string InitiatorText;
            public readonly TimeSpan StartTime;
            public readonly TimeSpan EndTime;
            public readonly HashSet<ICommonSession> VotesDirty = new();

            public bool Cancelled;
            public bool Finished;
            public bool Dirty = true;

            public VoteFinishedEventHandler? OnFinished;
            public VoteCancelledEventHandler? OnCancelled;
            public ICommonSession? Initiator { get; }

            public VoteReg(int id, VoteEntry[] entries, string title, string initiatorText,
                ICommonSession? initiator, TimeSpan start, TimeSpan end)
            {
                Id = id;
                Entries = entries;
                Title = title;
                InitiatorText = initiatorText;
                Initiator = initiator;
                StartTime = start;
                EndTime = end;
            }
        }

        private struct VoteEntry
        {
            public object Data;
            public string Text;
            public int Votes;

            public VoteEntry(object data, string text)
            {
                Data = data;
                Text = text;
                Votes = 0;
            }
        }

        #endregion

        #region IVoteHandle API surface

        private sealed class VoteHandle : IVoteHandle
        {
            private readonly VoteManager _mgr;
            private readonly VoteReg _reg;

            public int Id => _reg.Id;
            public string Title => _reg.Title;
            public string InitiatorText => _reg.InitiatorText;
            public bool Finished => _reg.Finished;
            public bool Cancelled => _reg.Cancelled;

            public IReadOnlyDictionary<object, int> VotesPerOption { get; }

            public event VoteFinishedEventHandler? OnFinished
            {
                add => _reg.OnFinished += value;
                remove => _reg.OnFinished -= value;
            }

            public event VoteCancelledEventHandler? OnCancelled
            {
                add => _reg.OnCancelled += value;
                remove => _reg.OnCancelled -= value;
            }

            public VoteHandle(VoteManager mgr, VoteReg reg)
            {
                _mgr = mgr;
                _reg = reg;

                VotesPerOption = new VoteDict(reg);
            }

            public bool IsValidOption(int optionId)
            {
                return _mgr.IsValidOption(_reg, optionId);
            }

            public void CastVote(ICommonSession session, int? optionId)
            {
                _mgr.CastVote(_reg, session, optionId);
            }

            public void Cancel()
            {
                _mgr.CancelVote(_reg);
            }

            private sealed class VoteDict : IReadOnlyDictionary<object, int>
            {
                private readonly VoteReg _reg;

                public VoteDict(VoteReg reg)
                {
                    _reg = reg;
                }

                public IEnumerator<KeyValuePair<object, int>> GetEnumerator()
                {
                    return _reg.Entries.Select(e => KeyValuePair.Create(e.Data, e.Votes)).GetEnumerator();
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return GetEnumerator();
                }

                public int Count => _reg.Entries.Length;

                public bool ContainsKey(object key)
                {
                    return TryGetValue(key, out _);
                }

                public bool TryGetValue(object key, out int value)
                {
                    var entry = _reg.Entries.FirstOrNull(a => a.Data.Equals(key));
                    if (entry != null)
                    {
                        value = entry.Value.Votes;
                        return true;
                    }

                    value = default;
                    return false;
                }

                public int this[object key]
                {
                    get
                    {
                        if (!TryGetValue(key, out var votes))
                        {
                            throw new KeyNotFoundException();
                        }

                        return votes;
                    }
                }

                public IEnumerable<object> Keys => _reg.Entries.Select(c => c.Data);
                public IEnumerable<int> Values => _reg.Entries.Select(c => c.Votes);
            }
        }

        #endregion
    }
}
