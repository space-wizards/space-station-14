using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Voting;
using Robust.Client;
using Robust.Client.Audio;
using Robust.Client.Console;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Sources;
using Robust.Shared.ContentPack;


namespace Content.Client.Voting
{
    public interface IVoteManager
    {
        void Initialize();
        void SendCastVote(int voteId, int option);
        void ClearPopupContainer();
        void SetPopupContainer(Control container);
        bool CanCallVote { get; }

        bool CanCallStandardVote(StandardVoteType type, out TimeSpan whenCan);
        event Action<bool> CanCallVoteChanged;
        event Action CanCallStandardVotesChanged;
    }

    public sealed class VoteManager : IVoteManager
    {
        [Dependency] private readonly IAudioManager _audio = default!;
        [Dependency] private readonly IBaseClient _client = default!;
        [Dependency] private readonly IClientConsoleHost _console = default!;
        [Dependency] private readonly IClientNetManager _netManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IResourceCache _res = default!;

        private readonly Dictionary<StandardVoteType, TimeSpan> _standardVoteTimeouts = new();
        private readonly Dictionary<int, ActiveVote> _votes = new();
        private readonly Dictionary<int, UI.VotePopup> _votePopups = new();
        private Control? _popupContainer;

        private IAudioSource? _voteSource;

        public bool CanCallVote { get; private set; }

        public event Action<bool>? CanCallVoteChanged;

        public event Action? CanCallStandardVotesChanged;

        public void Initialize()
        {
            const string sound = "/Audio/Effects/voteding.ogg";
            _voteSource = _audio.CreateAudioSource(_res.GetResource<AudioResource>(sound));

            if (_voteSource != null)
            {
                _voteSource.Global = true;
            }

            _netManager.RegisterNetMessage<MsgVoteData>(ReceiveVoteData);
            _netManager.RegisterNetMessage<MsgVoteCanCall>(ReceiveVoteCanCall);

            _client.RunLevelChanged += ClientOnRunLevelChanged;
        }

        private void ClientOnRunLevelChanged(object? sender, RunLevelChangedEventArgs e)
        {
            // Clear votes on disconnect.
            if (e.NewLevel == ClientRunLevel.Initialize)
            {
                ClearPopupContainer();
                _votes.Clear();
            }
        }

        public bool CanCallStandardVote(StandardVoteType type, out TimeSpan whenCan)
        {
            return !_standardVoteTimeouts.TryGetValue(type, out whenCan);
        }

        public void ClearPopupContainer()
        {
            if (_popupContainer == null)
                return;

            if (!_popupContainer.Disposed)
            {
                foreach (var popup in _votePopups.Values)
                {
                    popup.Orphan();
                }
            }

            _votePopups.Clear();
            _popupContainer = null;
        }

        public void SetPopupContainer(Control container)
        {
            if (_popupContainer != null)
            {
                ClearPopupContainer();
            }

            _popupContainer = container;
            SetVoteData();
        }

        private void SetVoteData()
        {
            if (_popupContainer == null)
                return;

            foreach (var (vId, vote) in _votes)
            {
                var popup = new UI.VotePopup(vote);

                _votePopups.Add(vId, popup);
                _popupContainer.AddChild(popup);
                popup.UpdateData();
            }
        }

        private void ReceiveVoteData(MsgVoteData message)
        {
            var @new = false;
            var voteId = message.VoteId;
            if (!_votes.TryGetValue(voteId, out var existingVote))
            {
                if (!message.VoteActive)
                {
                    // Got "vote inactive" for nonexistent vote???
                    return;
                }

                _voteSource?.Restart();
                @new = true;

                // Refresh
                var container = _popupContainer;
                ClearPopupContainer();

                if (container != null)
                    SetPopupContainer(container);

                // New vote from the server.
                var vote = new ActiveVote(voteId)
                {
                    Entries = message.Options
                        .Select(c => new VoteEntry(c.name))
                        .ToArray()
                };

                existingVote = vote;
                _votes.Add(voteId, vote);
            }
            else if (!message.VoteActive)
            {
                // Remove gone vote.
                _votes.Remove(voteId);
                if (_votePopups.TryGetValue(voteId, out var toRemove))
                {

                    toRemove.Orphan();
                    _votePopups.Remove(voteId);
                }

                return;
            }

            // Update vote data from incoming.
            if (message.IsYourVoteDirty)
                existingVote.OurVote = message.YourVote;
            // On the server, most of these params can't change.
            // It can't hurt to just re-set this stuff since I'm lazy and the server is sending it anyways, so...
            existingVote.Initiator = message.VoteInitiator;
            existingVote.Title = message.VoteTitle;
            existingVote.StartTime = _gameTiming.RealServerToLocal(message.StartTime);
            existingVote.EndTime = _gameTiming.RealServerToLocal(message.EndTime);

            // Logger.Debug($"{existingVote.StartTime}, {existingVote.EndTime}, {_gameTiming.RealTime}");

            for (var i = 0; i < message.Options.Length; i++)
            {
                existingVote.Entries[i].Votes = message.Options[i].votes;
            }

            if (@new && _popupContainer != null)
            {
                var popup = new UI.VotePopup(existingVote);

                _votePopups.Add(voteId, popup);
                _popupContainer.AddChild(popup);
            }

            if (_votePopups.TryGetValue(voteId, out var ePopup))
            {
                ePopup.UpdateData();
            }
        }

        private void ReceiveVoteCanCall(MsgVoteCanCall message)
        {
            if (CanCallVote != message.CanCall)
            {
                // TODO: actually use the "when can call vote" time for UI display or something.
                CanCallVote = message.CanCall;
                CanCallVoteChanged?.Invoke(CanCallVote);
            }

            _standardVoteTimeouts.Clear();
            foreach (var (type, time) in message.VotesUnavailable)
            {
                var fixedTime = (time == TimeSpan.Zero) ? time : _gameTiming.RealServerToLocal(time);
                _standardVoteTimeouts.Add(type, fixedTime);
            }

            CanCallStandardVotesChanged?.Invoke();
        }

        public void SendCastVote(int voteId, int option)
        {
            var data = _votes[voteId];
            // Update immediately to avoid any funny reconciliation bugs.
            // See also code in server side to avoid bulldozing this.
            data.OurVote = option;
            _console.LocalShell.RemoteExecuteCommand($"vote {voteId} {option}");
        }

        public sealed class ActiveVote
        {
            public VoteEntry[] Entries = default!;

            // Both of these are local RealTime (converted at NetMsg receive).
            public TimeSpan StartTime;
            public TimeSpan EndTime;
            public string Title = "";
            public string Initiator = "";
            public int? OurVote;
            public int Id;

            public ActiveVote(int voteId)
            {
                Id = voteId;
            }
        }

        public sealed class VoteEntry
        {
            public string Text { get; }
            public int Votes { get; set; }

            public VoteEntry(string text)
            {
                Text = text;
            }
        }
    }
}
