using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Database;
using Content.Server.Voting.Managers;
using Content.Server.Chat.Managers;
using Content.Server.Discord.WebhookMessages;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Server.Voting
{
    public sealed partial class CustomVoteSystem : EntitySystem
    {
        [Dependency] private IVoteManager _voteManager = default!;
        [Dependency] private IAdminLogManager _adminLogger = default!;
        [Dependency] private IChatManager _chatManager = default!;
        [Dependency] private VoteWebhooks _voteWebhooks = default!;
        [Dependency] private IConfigurationManager _cfg = default!;
        [Dependency] private IServerDbManager _dbManager = default!;
        [Dependency] private IEntitySystemManager _esm = default!;

        /// <summary>
        /// Starts a vote with custom options for some or all players
        /// </summary>
        /// <param name="initiator">who started the vote</param>
        /// <param name="showResultsInChat">if the results should be shown in chat</param>
        /// <param name="duration">how long the vote should last</param>
        /// <param name="title">the title of the vote</param>
        /// <param name="options">the list of options to vote from</param>
        /// <param name="players">list of players to show the vote for (all players if null)</param>
        public async void StartCustomVote(
            ICommonSession? initiator,
            bool showResultsInChat,
            float duration,
            string title,
            List<string> options,
            IEnumerable<EntityUid>? players = null)
        {
            if (options.Count is < 2 or > 9)
                return;

            var playerSessions = new HashSet<ICommonSession>();
            if (players != null)
            {
                foreach (var playerEnt in players)
                {
                    if (!TryComp<ActorComponent>(playerEnt, out var actor))
                        continue;

                    playerSessions.Add(actor.PlayerSession);
                }
            }

            var voteOptions = new VoteOptions
            {
                Title = title,
                Duration = TimeSpan.FromSeconds(duration),
                VoterEligibility = players != null ? VoteManager.VoterEligibility.SelectedPlayers : VoteManager.VoterEligibility.All,
                SelectedVoters = playerSessions,
            };

            for (var i = 0; i < options.Count; i++)
            {
                voteOptions.Options.Add((options[i], i+1));
            }

            voteOptions.SetInitiatorOrServer(initiator);

            if (initiator != null)
                _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"{initiator} initiated a custom vote: {voteOptions.Title} - {string.Join("; ", voteOptions.Options.Select(x => x.text))}");
            else
                _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"Initiated a custom vote: {voteOptions.Title} - {string.Join("; ", voteOptions.Options.Select(x => x.text))}");

            var vote = _voteManager.CreateVote(voteOptions);

            var voteLogId = await _dbManager.CustomVoteLogAdd(
                title,
                GameTicker.GetRoundId(_esm),
                initiator?.UserId,
                [..voteOptions.Options.Select(x => x.text)]);

            var webhookState = _voteWebhooks.CreateWebhookIfConfigured(voteOptions, _cfg.GetCVar(CCVars.DiscordVoteWebhook));

            vote.OnFinished += async (_, eventArgs) =>
            {
                if (eventArgs.Winner == null)
                {
                    var winners = voteOptions.Options
                        .Where(t => eventArgs.Winners.Contains(t.data))
                        .Select(t => t.text);
                    var ties = string.Join(", ", winners);
                    _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"Custom vote {voteOptions.Title} finished as tie: {ties}");

                    if (showResultsInChat)
                        _chatManager.DispatchServerAnnouncement(Loc.GetString("cmd-customvote-on-finished-tie", ("title", voteOptions.Title), ("ties", ties)));
                }
                else
                {
                    var winner = voteOptions.Options.FirstOrDefault(t => t.data == eventArgs.Winner).text;
                    _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"Custom vote {voteOptions.Title} finished: {winner}");

                    if (showResultsInChat)
                        _chatManager.DispatchServerAnnouncement(Loc.GetString("cmd-customvote-on-finished-win", ("title", voteOptions.Title), ("winner", winner)));
                }

                _voteWebhooks.UpdateWebhookIfConfigured(webhookState, eventArgs);
                await _dbManager.CustomVoteLogFinish(voteLogId, [..eventArgs.Votes]);
            };

            vote.OnCancelled += async _ =>
            {
                _voteWebhooks.UpdateCancelledWebhookIfConfigured(webhookState);
                await _dbManager.CustomVoteLogCancel(voteLogId);
            };
        }
    }
}
