using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Administration;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameTicking;
using Content.Shared.Network.NetMessages;
using Content.Shared.Utility;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Timing;

#nullable enable

namespace Content.Server.Voting
{
    public sealed class VoteManager : IVoteManager
    {
        [Dependency] private readonly IServerNetManager _netManager = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IGameTicker _ticker = default!;
        [Dependency] private readonly IAdminManager _adminMgr = default!;

        private int _nextVoteId = 1;

        private readonly Dictionary<int, VoteReg> _votes = new();
        private readonly Dictionary<int, VoteHandle> _voteHandles = new();

        private readonly Dictionary<NetUserId, TimeSpan> _lastVoteTime = new();

        public void Initialize()
        {
            _netManager.RegisterNetMessage<MsgVoteData>(MsgVoteData.NAME);

            _playerManager.PlayerStatusChanged += PlayerManagerOnPlayerStatusChanged;
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

        private void CastVote(VoteReg v, IPlayerSession player, int? option)
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

            // TODO: timeout players.

            return handle;
        }

        private void SendUpdates(VoteReg v)
        {
            foreach (var player in _playerManager.GetAllPlayers())
            {
                SendSingleUpdate(v, player);
            }

            v.VotesDirty.Clear();
            v.Dirty = false;
        }

        private void SendSingleUpdate(VoteReg v, IPlayerSession player)
        {
            var msg = _netManager.CreateNetMessage<MsgVoteData>();

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

        private void EndVote(VoteReg v)
        {
            if (v.Finished)
            {
                return;
            }

            // Find winner or stalemate.
            ref var winningEntry = ref v.Entries[0];
            var stalemate = false;
            for (var i = 1; i < v.Entries.Length; i++)
            {
                ref var contender = ref v.Entries[i];

                if (contender.Votes == winningEntry.Votes)
                {
                    stalemate = true;
                }
                else if (contender.Votes > winningEntry.Votes)
                {
                    stalemate = false;
                    winningEntry = contender;
                }
            }

            v.Finished = true;
            v.Dirty = true;
            var args = new VoteFinishedEventArgs(stalemate ? null : winningEntry.Data);
            v.OnFinished?.Invoke(_voteHandles[v.Id], args);
        }

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

        #region Preset Votes

        public void CreateRestartVote(IPlayerSession? initiator)
        {
            var options = new VoteOptions
            {
                Title = Loc.GetString("Restart round"),
                Options =
                {
                    (Loc.GetString("Yes"), true),
                    (Loc.GetString("No"), false)
                },
                Duration = _playerManager.PlayerCount == 1 && initiator != null
                    ? TimeSpan.FromSeconds(10)
                    : TimeSpan.FromSeconds(30)
            };

            WirePresetVoteInitiator(options, initiator);

            var vote = CreateVote(options);

            vote.OnFinished += (_, args) =>
            {
                if (args.Winner == null)
                {
                    _chatManager.DispatchServerAnnouncement(Loc.GetString("Restart vote failed due to stalemate."));
                    return;
                }

                var win = (bool) args.Winner;
                if (win)
                {
                    _chatManager.DispatchServerAnnouncement(Loc.GetString("Restart vote succeeded."));
                    _ticker.RestartRound();
                }
                else
                {
                    _chatManager.DispatchServerAnnouncement(Loc.GetString("Restart vote failed."));
                }
            };

            if (initiator != null)
            {
                // Cast yes vote if created the vote yourself.
                vote.CastVote(initiator, 0);
            }
        }

        private void WirePresetVoteInitiator(VoteOptions options, IPlayerSession? player)
        {
            if (player != null)
            {
                options.SetInitiator(player);
            }
            else
            {
                options.InitiatorText = Loc.GetString("The server");
            }
        }

        #endregion

        #region Vote Data

        private sealed class VoteReg
        {
            public readonly int Id;
            public readonly Dictionary<IPlayerSession, int> CastVotes = new();
            public readonly VoteEntry[] Entries;
            public readonly string Title;
            public readonly string InitiatorText;
            public readonly TimeSpan StartTime;
            public readonly TimeSpan EndTime;
            public readonly HashSet<IPlayerSession> VotesDirty = new();

            public bool Finished;
            public bool Dirty = true;

            public VoteFinishedEventHandler? OnFinished;
            public IPlayerSession? Initiator { get; }

            public VoteReg(int id, VoteEntry[] entries, string title, string initiatorText,
                IPlayerSession? initiator, TimeSpan start, TimeSpan end)
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
            public object? Data;
            public string Text;
            public int Votes;

            public VoteEntry(object? data, string text)
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

            public event VoteFinishedEventHandler? OnFinished
            {
                add => _reg.OnFinished += value;
                remove => _reg.OnFinished -= value;
            }

            public VoteHandle(VoteManager mgr, VoteReg reg)
            {
                _mgr = mgr;
                _reg = reg;
            }

            public bool IsValidOption(int optionId)
            {
                return _mgr.IsValidOption(_reg, optionId);
            }

            public void CastVote(IPlayerSession session, int? optionId)
            {
                _mgr.CastVote(_reg, session, optionId);
            }
        }

        #endregion
    }
}
