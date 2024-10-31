using System.Linq;
using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Server.Discord.WebhookMessages;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Presets;
using Content.Server.Maps;
using Content.Server.Roles;
using Content.Server.RoundEnd;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.Players;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Voting;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Voting.Managers
{
    public sealed partial class VoteManager
    {
        [Dependency] private readonly IPlayerLocator _locator = default!;
        [Dependency] private readonly ILogManager _logManager = default!;
        [Dependency] private readonly IBanManager _bans = default!;
        [Dependency] private readonly IServerDbManager _dbManager = default!;
        [Dependency] private readonly VoteWebhooks _voteWebhooks = default!;

        private VotingSystem? _votingSystem;
        private RoleSystem? _roleSystem;

        private static readonly Dictionary<StandardVoteType, CVarDef<bool>> _voteTypesToEnableCVars = new()
        {
            {StandardVoteType.Restart, CCVars.VoteRestartEnabled},
            {StandardVoteType.Preset, CCVars.VotePresetEnabled},
            {StandardVoteType.Map, CCVars.VoteMapEnabled},
            {StandardVoteType.Votekick, CCVars.VotekickEnabled}
        };

        public void CreateStandardVote(ICommonSession? initiator, StandardVoteType voteType, string[]? args = null)
        {
            if (initiator != null && args == null)
                _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"{initiator} initiated a {voteType.ToString()} vote");
            else if (initiator != null && args != null)
                _adminLogger.Add(LogType.Vote, LogImpact.Extreme, $"{initiator} initiated a {voteType.ToString()} vote with the arguments: {String.Join(",", args)}");
            else
                _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"Initiated a {voteType.ToString()} vote");

            bool timeoutVote = true;

            switch (voteType)
            {
                case StandardVoteType.Restart:
                    CreateRestartVote(initiator);
                    break;
                case StandardVoteType.Preset:
                    CreatePresetVote(initiator);
                    break;
                case StandardVoteType.Map:
                    CreateMapVote(initiator);
                    break;
                case StandardVoteType.Votekick:
                    timeoutVote = false; // Allows the timeout to be updated manually in the create method
                    CreateVotekickVote(initiator, args);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(voteType), voteType, null);
            }
            var ticker = _entityManager.EntitySysManager.GetEntitySystem<GameTicker>();
            ticker.UpdateInfoText();
            if (timeoutVote)
                TimeoutStandardVote(voteType);
        }

        private void CreateRestartVote(ICommonSession? initiator)
        {

            var playerVoteMaximum = _cfg.GetCVar(CCVars.VoteRestartMaxPlayers);
            var totalPlayers = _playerManager.Sessions.Count(session => session.Status != SessionStatus.Disconnected);

            var ghostVotePercentageRequirement = _cfg.GetCVar(CCVars.VoteRestartGhostPercentage);
            var ghostVoterPercentage = CalculateEligibleVoterPercentage(VoterEligibility.Ghost);

            if (totalPlayers <= playerVoteMaximum || ghostVoterPercentage >= ghostVotePercentageRequirement)
            {
                StartVote(initiator);
            }
            else
            {
                NotifyNotEnoughGhostPlayers(ghostVotePercentageRequirement, ghostVoterPercentage);
            }
        }

        /// <summary>
        /// Gives the current percentage of players eligible to vote, rounded to nearest percentage point.
        /// </summary>
        /// <param name="eligibility">The eligibility requirement to vote.</param>
        public int CalculateEligibleVoterPercentage(VoterEligibility eligibility)
        {
            var eligibleCount = CalculateEligibleVoterNumber(eligibility);
            var totalPlayers = _playerManager.Sessions.Count(session => session.Status != SessionStatus.Disconnected);

            var eligiblePercentage = 0.0;
            if (totalPlayers > 0)
            {
                eligiblePercentage = ((double)eligibleCount / totalPlayers) * 100;
            }

            var roundedEligiblePercentage = (int)Math.Round(eligiblePercentage);

            return roundedEligiblePercentage;
        }

        /// <summary>
        /// Gives the current number of players eligible to vote.
        /// </summary>
        /// <param name="eligibility">The eligibility requirement to vote.</param>
        public int CalculateEligibleVoterNumber(VoterEligibility eligibility)
        {
            var eligibleCount = 0;

            foreach (var player in _playerManager.Sessions)
            {
                _playerManager.UpdateState(player);
                if (player.Status != SessionStatus.Disconnected && CheckVoterEligibility(player, eligibility))
                {
                    eligibleCount++;
                }
            }

            return eligibleCount;
        }

        private void StartVote(ICommonSession? initiator)
        {
            var alone = _playerManager.PlayerCount == 1 && initiator != null;
            var options = new VoteOptions
            {
                Title = Loc.GetString("ui-vote-restart-title"),
                Options =
                {
                    (Loc.GetString("ui-vote-restart-yes"), "yes"),
                    (Loc.GetString("ui-vote-restart-no"), "no"),
                    (Loc.GetString("ui-vote-restart-abstain"), "abstain")
                },
                Duration = alone
                    ? TimeSpan.FromSeconds(_cfg.GetCVar(CCVars.VoteTimerAlone))
                    : TimeSpan.FromSeconds(_cfg.GetCVar(CCVars.VoteTimerRestart)),
                InitiatorTimeout = TimeSpan.FromMinutes(5)
            };

            if (alone)
                options.InitiatorTimeout = TimeSpan.FromSeconds(10);

            WirePresetVoteInitiator(options, initiator);

            var vote = CreateVote(options);

            vote.OnFinished += (_, _) =>
            {
                var votesYes = vote.VotesPerOption["yes"];
                var votesNo = vote.VotesPerOption["no"];
                var total = votesYes + votesNo;

                var ratioRequired = _cfg.GetCVar(CCVars.VoteRestartRequiredRatio);
                if (total > 0 && votesYes / (float) total >= ratioRequired)
                {
                    // Check if an admin is online, and ignore the passed vote if the cvar is enabled
                    if (_cfg.GetCVar(CCVars.VoteRestartNotAllowedWhenAdminOnline) && _adminMgr.ActiveAdmins.Count() != 0)
                    {
                        _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"Restart vote attempted to pass, but an admin was online. {votesYes}/{votesNo}");
                    }
                    else // If the cvar is disabled or there's no admins on, proceed as normal
                    {
                        _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"Restart vote succeeded: {votesYes}/{votesNo}");
                        _chatManager.DispatchServerAnnouncement(Loc.GetString("ui-vote-restart-succeeded"));
                        var roundEnd = _entityManager.EntitySysManager.GetEntitySystem<RoundEndSystem>();
                        roundEnd.EndRound();
                    }
                }
                else
                {
                    _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"Restart vote failed: {votesYes}/{votesNo}");
                    _chatManager.DispatchServerAnnouncement(
                        Loc.GetString("ui-vote-restart-failed", ("ratio", ratioRequired)));
                }
            };

            if (initiator != null)
            {
                // Cast yes vote if created the vote yourself.
                vote.CastVote(initiator, 0);
            }

            foreach (var player in _playerManager.Sessions)
            {
                if (player != initiator)
                {
                    // Everybody else defaults to an abstain vote to say they don't mind.
                    vote.CastVote(player, 2);
                }
            }
        }

        private void NotifyNotEnoughGhostPlayers(int ghostPercentageRequirement, int roundedGhostPercentage)
        {
            // Logic to notify that there are not enough ghost players to start a vote
            _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"Restart vote failed: Current Ghost player percentage:{roundedGhostPercentage.ToString()}% does not meet {ghostPercentageRequirement.ToString()}%");
            _chatManager.DispatchServerAnnouncement(
                Loc.GetString("ui-vote-restart-fail-not-enough-ghost-players", ("ghostPlayerRequirement", ghostPercentageRequirement)));
        }

        private void CreatePresetVote(ICommonSession? initiator)
        {
            var presets = GetGamePresets();

            var alone = _playerManager.PlayerCount == 1 && initiator != null;
            var options = new VoteOptions
            {
                Title = Loc.GetString("ui-vote-gamemode-title"),
                Duration = alone
                    ? TimeSpan.FromSeconds(_cfg.GetCVar(CCVars.VoteTimerAlone))
                    : TimeSpan.FromSeconds(_cfg.GetCVar(CCVars.VoteTimerPreset))
            };

            if (alone)
                options.InitiatorTimeout = TimeSpan.FromSeconds(10);

            foreach (var (k, v) in presets)
            {
                options.Options.Add((Loc.GetString(v), k));
            }

            WirePresetVoteInitiator(options, initiator);

            var vote = CreateVote(options);

            vote.OnFinished += (_, args) =>
            {
                string picked;
                if (args.Winner == null)
                {
                    picked = (string) _random.Pick(args.Winners);
                    _chatManager.DispatchServerAnnouncement(
                        Loc.GetString("ui-vote-gamemode-tie", ("picked", Loc.GetString(presets[picked]))));
                }
                else
                {
                    picked = (string) args.Winner;
                    _chatManager.DispatchServerAnnouncement(
                        Loc.GetString("ui-vote-gamemode-win", ("winner", Loc.GetString(presets[picked]))));
                }
                _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"Preset vote finished: {picked}");
                var ticker = _entityManager.EntitySysManager.GetEntitySystem<GameTicker>();
                ticker.SetGamePreset(picked);
            };
        }

        private void CreateMapVote(ICommonSession? initiator)
        {
            var maps = _gameMapManager.CurrentlyEligibleMaps().ToDictionary(map => map, map => map.MapName);

            var alone = _playerManager.PlayerCount == 1 && initiator != null;
            var options = new VoteOptions
            {
                Title = Loc.GetString("ui-vote-map-title"),
                Duration = alone
                    ? TimeSpan.FromSeconds(_cfg.GetCVar(CCVars.VoteTimerAlone))
                    : TimeSpan.FromSeconds(_cfg.GetCVar(CCVars.VoteTimerMap))
            };

            if (alone)
                options.InitiatorTimeout = TimeSpan.FromSeconds(10);

            foreach (var (k, v) in maps)
            {
                options.Options.Add((v, k));
            }

            WirePresetVoteInitiator(options, initiator);

            var vote = CreateVote(options);

            vote.OnFinished += (_, args) =>
            {
                GameMapPrototype picked;
                if (args.Winner == null)
                {
                    picked = (GameMapPrototype) _random.Pick(args.Winners);
                    _chatManager.DispatchServerAnnouncement(
                        Loc.GetString("ui-vote-map-tie", ("picked", maps[picked])));
                }
                else
                {
                    picked = (GameMapPrototype) args.Winner;
                    _chatManager.DispatchServerAnnouncement(
                        Loc.GetString("ui-vote-map-win", ("winner", maps[picked])));
                }

                _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"Map vote finished: {picked.MapName}");
                var ticker = _entityManager.EntitySysManager.GetEntitySystem<GameTicker>();
                if (ticker.CanUpdateMap())
                {
                    if (_gameMapManager.TrySelectMapIfEligible(picked.ID))
                    {
                        ticker.UpdateInfoText();
                    }
                }
                else
                {
                    if (ticker.RoundPreloadTime <= TimeSpan.Zero)
                    {
                        _chatManager.DispatchServerAnnouncement(Loc.GetString("ui-vote-map-notlobby"));
                    }
                    else
                    {
                        var timeString = $"{ticker.RoundPreloadTime.Minutes:0}:{ticker.RoundPreloadTime.Seconds:00}";
                        _chatManager.DispatchServerAnnouncement(Loc.GetString("ui-vote-map-notlobby-time", ("time", timeString)));
                    }
                }
            };
        }

        private async void CreateVotekickVote(ICommonSession? initiator, string[]? args)
        {
            if (args == null || args.Length <= 1)
            {
                return;
            }

            if (_roleSystem == null)
                _roleSystem = _entityManager.SystemOrNull<RoleSystem>();
            if (_votingSystem == null)
                _votingSystem = _entityManager.SystemOrNull<VotingSystem>();

            // Check that the initiator is actually allowed to do a votekick.
            if (_votingSystem != null && !await _votingSystem.CheckVotekickInitEligibility(initiator))
            {
                _logManager.GetSawmill("admin.votekick").Warning($"User {initiator} attempted a votekick, despite not being eligible to!");
                _adminLogger.Add(LogType.Vote, LogImpact.Extreme, $"Votekick attempted by {initiator}, but they are not eligible to votekick!");
                DirtyCanCallVoteAll();
                return;
            }

            var eligibleVoterNumberRequirement = _cfg.GetCVar(CCVars.VotekickEligibleNumberRequirement);
            var eligibleVoterNumber = _cfg.GetCVar(CCVars.VotekickVoterGhostRequirement) ? CalculateEligibleVoterNumber(VoterEligibility.GhostMinimumPlaytime) : CalculateEligibleVoterNumber(VoterEligibility.MinimumPlaytime);

            string target = args[0];
            string reason = args[1];

            // Start by getting all relevant target data
            var located = await _locator.LookupIdByNameOrIdAsync(target);
            if (located == null)
            {
                _logManager.GetSawmill("admin.votekick")
                    .Warning($"Votekick attempted for player {target} but they couldn't be found!");
                _adminLogger.Add(LogType.Vote, LogImpact.Extreme, $"Votekick attempted by {initiator} for player string {target}, but they could not be found!");
                DirtyCanCallVoteAll();
                return;
            }
            var targetUid = located.UserId;
            var targetHWid = located.LastHWId;
            if (!_playerManager.TryGetSessionById(located.UserId, out ICommonSession? targetSession))
            {
                _logManager.GetSawmill("admin.votekick")
                    .Warning($"Votekick attempted for player {target} but their session couldn't be found!");
                _adminLogger.Add(LogType.Vote, LogImpact.Extreme, $"Votekick attempted by {initiator} for player string {target}, but they could not be found!");
                DirtyCanCallVoteAll();
                return;
            }

            string targetEntityName = located.Username; // Target's player-facing name when voting; uses the player's username as fallback if no entity name is found
            if (targetSession.AttachedEntity is { Valid: true } attached && _votingSystem != null)
                targetEntityName = _votingSystem.GetPlayerVoteListName(attached);

            var isAntagSafe = false;
            var targetMind = targetSession.GetMind();
            var playtime = _playtimeManager.GetPlayTimes(targetSession);

            // Check whether the target is an antag, and if they are, give them protection against the Raider votekick if they have the requisite hours.
            if (targetMind != null &&
                _roleSystem != null &&
                _roleSystem.MindIsAntagonist(targetMind) &&
                playtime.TryGetValue(PlayTimeTrackingShared.TrackerOverall, out TimeSpan overallTime) &&
                overallTime >= TimeSpan.FromHours(_cfg.GetCVar(CCVars.VotekickAntagRaiderProtection)))
            {
                isAntagSafe = true;
            }


            // Don't let a user votekick themselves
            if (initiator == targetSession)
            {
                _adminLogger.Add(LogType.Vote, LogImpact.Extreme, $"Votekick attempted by {initiator} for themselves? Votekick cancelled.");
                DirtyCanCallVoteAll();
                return;
            }

            // Cancels the vote if there's not enough voters; only the person initiating the vote gets a return message.
            if (eligibleVoterNumber < eligibleVoterNumberRequirement)
            {
                _adminLogger.Add(LogType.Vote, LogImpact.Extreme, $"Votekick attempted by {initiator} for player {targetSession}, but there were not enough ghost roles! {eligibleVoterNumberRequirement} required, {eligibleVoterNumber} found.");
                if (initiator != null)
                {
                    var message = Loc.GetString("ui-vote-votekick-not-enough-eligible", ("voters", eligibleVoterNumber.ToString()), ("requirement", eligibleVoterNumberRequirement.ToString()));
                    var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
                    _chatManager.ChatMessageToOne(ChatChannel.Server, message, wrappedMessage, default, false, initiator.Channel);
                }
                DirtyCanCallVoteAll();
                return;
            }

            // Check for stuff like the target being an admin. These targets shouldn't show up in the UI, but it's necessary to doublecheck in case someone writes the command in console.
            if (_votingSystem != null && !_votingSystem.CheckVotekickTargetEligibility(targetSession))
            {
                _adminLogger.Add(LogType.Vote, LogImpact.Extreme, $"Votekick attempted by {initiator} for player {targetSession}, but they are not eligible to be votekicked!");
                DirtyCanCallVoteAll();
                return;
            }

            // Create the vote object

            string voteTitle = "";
            NetEntity? targetNetEntity = _entityManager.GetNetEntity(targetSession.AttachedEntity);
            var initiatorName = initiator != null ? initiator.Name : Loc.GetString("ui-vote-votekick-unknown-initiator");

            voteTitle = Loc.GetString("ui-vote-votekick-title", ("initiator", initiatorName), ("targetEntity", targetEntityName), ("reason", reason));

            var options = new VoteOptions
            {
                Title = voteTitle,
                Options =
                {
                    (Loc.GetString("ui-vote-votekick-yes"), "yes"),
                    (Loc.GetString("ui-vote-votekick-no"), "no"),
                    (Loc.GetString("ui-vote-votekick-abstain"), "abstain")
                },
                Duration = TimeSpan.FromSeconds(_cfg.GetCVar(CCVars.VotekickTimer)),
                InitiatorTimeout = TimeSpan.FromMinutes(_cfg.GetCVar(CCVars.VotekickTimeout)),
                VoterEligibility = _cfg.GetCVar(CCVars.VotekickVoterGhostRequirement) ? VoterEligibility.GhostMinimumPlaytime : VoterEligibility.MinimumPlaytime,
                DisplayVotes = false,
                TargetEntity = targetNetEntity
            };

            WirePresetVoteInitiator(options, initiator);

            var vote = CreateVote(options);
            _adminLogger.Add(LogType.Vote, LogImpact.Extreme, $"Votekick for {located.Username} ({targetEntityName}) due to {reason} started, initiated by {initiator}.");

            // Create Discord webhook
            var webhookState = _voteWebhooks.CreateWebhookIfConfigured(options, _cfg.GetCVar(CCVars.DiscordVotekickWebhook), Loc.GetString("votekick-webhook-name"), options.Title + "\n" + Loc.GetString("votekick-webhook-description", ("initiator", initiatorName), ("target", targetSession)));

            // Time out the vote now that we know it will happen
            TimeoutStandardVote(StandardVoteType.Votekick);

            vote.OnFinished += (_, eventArgs) =>
            {

                var votesYes = vote.VotesPerOption["yes"];
                var votesNo = vote.VotesPerOption["no"];
                var total = votesYes + votesNo;

                // Get the voters, for logging purposes.
                List<ICommonSession> yesVoters = new();
                List<ICommonSession> noVoters = new();
                foreach (var (voter, castVote) in vote.CastVotes)
                {
                    if (castVote == 0)
                    {
                        yesVoters.Add(voter);
                    }
                    if (castVote == 1)
                    {
                        noVoters.Add(voter);
                    }
                }
                var yesVotersString = string.Join(", ", yesVoters);
                var noVotersString = string.Join(", ", noVoters);

                var ratioRequired = _cfg.GetCVar(CCVars.VotekickRequiredRatio);
                if (total > 0 && votesYes / (float)total >= ratioRequired)
                {
                    // Some conditions that cancel the vote want to let the vote run its course first and then cancel it
                    // so we check for that here

                    // Check if an admin is online, and ignore the vote if the cvar is enabled
                    if (_cfg.GetCVar(CCVars.VotekickNotAllowedWhenAdminOnline) && _adminMgr.ActiveAdmins.Count() != 0)
                    {
                        _adminLogger.Add(LogType.Vote, LogImpact.Extreme, $"Votekick for {located.Username} attempted to pass, but an admin was online. Yes: {votesYes} / No: {votesNo}. Yes: {yesVotersString} / No: {noVotersString}");
                        AnnounceCancelledVotekickForVoters(targetEntityName);
                        _voteWebhooks.UpdateCancelledWebhookIfConfigured(webhookState, Loc.GetString("votekick-webhook-cancelled-admin-online"));
                        return;
                    }
                    // Check if the target is an antag and the vote reason is raiding (this is to prevent false positives)
                    else if (isAntagSafe && reason == VotekickReasonType.Raiding.ToString())
                    {
                        _adminLogger.Add(LogType.Vote, LogImpact.Extreme, $"Votekick for {located.Username} due to {reason} finished, created by {initiator}, but was cancelled due to the target being an antagonist.");
                        AnnounceCancelledVotekickForVoters(targetEntityName);
                        _voteWebhooks.UpdateCancelledWebhookIfConfigured(webhookState, Loc.GetString("votekick-webhook-cancelled-antag-target"));
                        return;
                    }
                    // Check if the target is an admin/de-admined admin
                    else if (targetSession.AttachedEntity != null && _adminMgr.IsAdmin(targetSession.AttachedEntity.Value, true))
                    {
                        _adminLogger.Add(LogType.Vote, LogImpact.Extreme, $"Votekick for {located.Username} due to {reason} finished, created by {initiator}, but was cancelled due to the target being a de-admined admin.");
                        AnnounceCancelledVotekickForVoters(targetEntityName);
                        _voteWebhooks.UpdateCancelledWebhookIfConfigured(webhookState, Loc.GetString("votekick-webhook-cancelled-admin-target"));
                        return;
                    }
                    else
                    {
                        _adminLogger.Add(LogType.Vote, LogImpact.Extreme, $"Votekick for {located.Username} succeeded:  Yes: {votesYes} / No: {votesNo}. Yes: {yesVotersString} / No: {noVotersString}");
                        _chatManager.DispatchServerAnnouncement(Loc.GetString("ui-vote-votekick-success", ("target", targetEntityName), ("reason", reason)));

                        if (!Enum.TryParse(_cfg.GetCVar(CCVars.VotekickBanDefaultSeverity), out NoteSeverity severity))
                        {
                            _logManager.GetSawmill("admin.votekick")
                                .Warning("Votekick ban severity could not be parsed from config! Defaulting to high.");
                            severity = NoteSeverity.High;
                        }

                        // Discord webhook, success
                        _voteWebhooks.UpdateWebhookIfConfigured(webhookState, eventArgs);

                        uint minutes = (uint)_cfg.GetCVar(CCVars.VotekickBanDuration);

                        _bans.CreateServerBan(targetUid, target, null, null, targetHWid, minutes, severity, reason);
                    }
                }
                else
                {

                    // Discord webhook, failure
                    _voteWebhooks.UpdateWebhookIfConfigured(webhookState, eventArgs);

                    _adminLogger.Add(LogType.Vote, LogImpact.Extreme, $"Votekick failed: Yes: {votesYes} / No: {votesNo}. Yes: {yesVotersString} / No: {noVotersString}");
                    _chatManager.DispatchServerAnnouncement(Loc.GetString("ui-vote-votekick-failure", ("target", targetEntityName), ("reason", reason)));
                }
            };

            if (initiator != null)
            {
                // Cast yes vote if created the vote yourself.
                vote.CastVote(initiator, 0);
            }
        }

        private void AnnounceCancelledVotekickForVoters(string target)
        {
            foreach (var player in _playerManager.Sessions)
            {
                if (CheckVoterEligibility(player, VoterEligibility.GhostMinimumPlaytime))
                {
                    var message = Loc.GetString("ui-vote-votekick-server-cancelled", ("target", target));
                    var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
                    _chatManager.ChatMessageToOne(ChatChannel.Server, message, wrappedMessage, default, false, player.Channel);
                }
            }
        }

        private void TimeoutStandardVote(StandardVoteType type)
        {
            var timeout = TimeSpan.FromSeconds(_cfg.GetCVar(CCVars.VoteSameTypeTimeout));
            _standardVoteTimeout[type] = _timing.RealTime + timeout;
            DirtyCanCallVoteAll();
        }

        private Dictionary<string, string> GetGamePresets()
        {
            var presets = new Dictionary<string, string>();

            foreach (var preset in _prototypeManager.EnumeratePrototypes<GamePresetPrototype>())
            {
                if(!preset.ShowInVote)
                    continue;

                if(_playerManager.PlayerCount < (preset.MinPlayers ?? int.MinValue))
                    continue;

                if(_playerManager.PlayerCount > (preset.MaxPlayers ?? int.MaxValue))
                    continue;

                presets[preset.ID] = preset.ModeTitle;
            }
            return presets;
        }
    }
}
