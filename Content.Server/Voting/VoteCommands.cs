using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.Discord;
using Content.Server.GameTicking;
using Content.Server.Voting.Managers;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Voting;
using Robust.Server;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Utility;

namespace Content.Server.Voting
{
    [AnyCommand]
    public sealed class CreateVoteCommand : IConsoleCommand
    {
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;

        public string Command => "createvote";
        public string Description => Loc.GetString("cmd-createvote-desc");
        public string Help => Loc.GetString("cmd-createvote-help");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteError(Loc.GetString("shell-need-exactly-one-argument"));
                return;
            }

            if (!Enum.TryParse<StandardVoteType>(args[0], ignoreCase: true, out var type))
            {
                shell.WriteError(Loc.GetString("cmd-createvote-invalid-vote-type"));
                return;
            }

            var mgr = IoCManager.Resolve<IVoteManager>();

            if (shell.Player != null && !mgr.CanCallVote(shell.Player, type))
            {
                _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"{shell.Player} failed to start {type.ToString()} vote");
                shell.WriteError(Loc.GetString("cmd-createvote-cannot-call-vote-now"));
                return;
            }

            mgr.CreateStandardVote(shell.Player, type);
        }

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
            {
                var options = Enum.GetNames<StandardVoteType>();
                return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-createvote-arg-vote-type"));
            }

            return CompletionResult.Empty;
        }
    }

    [AdminCommand(AdminFlags.Moderator)]
    public sealed class CreateCustomCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly IVoteManager _voteManager = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly DiscordWebhook _discord = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;
        [Dependency] private readonly IBaseServer _baseServer = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;

        private ISawmill _sawmill = default!;

        private const int MaxArgCount = 10;

        public override string Command => "customvote";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            _sawmill = Logger.GetSawmill("vote");

            if (args.Length < 3 || args.Length > MaxArgCount)
            {
                shell.WriteError(Loc.GetString("shell-need-between-arguments",("lower", 3), ("upper", 10)));
                return;
            }

            var title = args[0];

            var options = new VoteOptions
            {
                Title = title,
                Duration = TimeSpan.FromSeconds(30),
            };

            for (var i = 1; i < args.Length; i++)
            {
                options.Options.Add((args[i], i));
            }

            options.SetInitiatorOrServer(shell.Player);

            if (shell.Player != null)
                _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"{shell.Player} initiated a custom vote: {options.Title} - {string.Join("; ", options.Options.Select(x => x.text))}");
            else
                _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"Initiated a custom vote: {options.Title} - {string.Join("; ", options.Options.Select(x => x.text))}");

            var vote = _voteManager.CreateVote(options);

            var webhookState = CreateWebhookIfConfigured(options);

            vote.OnFinished += (_, eventArgs) =>
            {
                if (eventArgs.Winner == null)
                {
                    var ties = string.Join(", ", eventArgs.Winners.Select(c => args[(int) c]));
                    _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"Custom vote {options.Title} finished as tie: {ties}");
                    _chatManager.DispatchServerAnnouncement(Loc.GetString("cmd-customvote-on-finished-tie", ("ties", ties)));
                }
                else
                {
                    _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"Custom vote {options.Title} finished: {args[(int) eventArgs.Winner]}");
                    _chatManager.DispatchServerAnnouncement(Loc.GetString("cmd-customvote-on-finished-win", ("winner", args[(int) eventArgs.Winner])));
                }

                UpdateWebhookIfConfigured(webhookState, eventArgs);
            };

            vote.OnCancelled += _ =>
            {
                UpdateCancelledWebhookIfConfigured(webhookState);
            };
        }

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
                return CompletionResult.FromHint(Loc.GetString("cmd-customvote-arg-title"));

            if (args.Length > MaxArgCount)
                return CompletionResult.Empty;

            var n = args.Length - 1;
            return CompletionResult.FromHint(Loc.GetString("cmd-customvote-arg-option-n", ("n", n)));
        }

        private WebhookState? CreateWebhookIfConfigured(VoteOptions voteOptions)
        {
            // All this webhook code is complete garbage.
            // I tried to clean it up somewhat, at least to fix the glaring bugs in it.
            // Jesus christ man what is with our code review process.

            var webhookUrl = _cfg.GetCVar(CCVars.DiscordVoteWebhook);
            if (string.IsNullOrEmpty(webhookUrl))
                return null;

            // Set up the webhook payload
            var serverName = _baseServer.ServerName;

            var fields = new List<WebhookEmbedField>();

            foreach (var voteOption in voteOptions.Options)
            {
                var newVote = new WebhookEmbedField
                {
                    Name = voteOption.text,
                    Value = Loc.GetString("custom-vote-webhook-option-pending")
                };
                fields.Add(newVote);
            }

            var runLevel = Loc.GetString($"game-run-level-{_gameTicker.RunLevel}");

            var payload = new WebhookPayload()
            {
                Username = Loc.GetString("custom-vote-webhook-name"),
                Embeds = new List<WebhookEmbed>
                {
                    new()
                    {
                        Title = voteOptions.InitiatorText,
                        Color = 13438992, // #CD1010
                        Description = voteOptions.Title,
                        Footer = new WebhookEmbedFooter
                        {
                            Text = Loc.GetString(
                                "custom-vote-webhook-footer",
                                ("serverName", serverName),
                                ("roundId", _gameTicker.RoundId),
                                ("runLevel", runLevel)),
                        },

                        Fields = fields,
                    },
                },
            };

            var state = new WebhookState
            {
                WebhookUrl = webhookUrl,
                Payload = payload,
            };

            CreateWebhookMessage(state, payload);

            return state;
        }

        private void UpdateWebhookIfConfigured(WebhookState? state, VoteFinishedEventArgs finished)
        {
            if (state == null)
                return;

            var embed = state.Payload.Embeds![0];
            embed.Color = 2353993; // #23EB49

            for (var i = 0; i < finished.Votes.Count; i++)
            {
                var oldName = embed.Fields[i].Name;
                var newValue = finished.Votes[i].ToString();
                embed.Fields[i] = new WebhookEmbedField { Name = oldName, Value = newValue, Inline =  true};
            }

            state.Payload.Embeds[0] = embed;

            UpdateWebhookMessage(state, state.Payload, state.MessageId);
        }

        private void UpdateCancelledWebhookIfConfigured(WebhookState? state)
        {
            if (state == null)
                return;

            var embed = state.Payload.Embeds![0];
            embed.Color = 13356304; // #CBCD10
            embed.Description += "\n\n" + Loc.GetString("custom-vote-webhook-cancelled");

            for (var i = 0; i < embed.Fields.Count; i++)
            {
                var oldName = embed.Fields[i].Name;
                embed.Fields[i] = new WebhookEmbedField { Name = oldName, Value = Loc.GetString("custom-vote-webhook-option-cancelled"), Inline =  true};
            }

            state.Payload.Embeds[0] = embed;

            UpdateWebhookMessage(state, state.Payload, state.MessageId);
        }

        // Sends the payload's message.
        private async void CreateWebhookMessage(WebhookState state, WebhookPayload payload)
        {
            try
            {
                if (await _discord.GetWebhook(state.WebhookUrl) is not { } identifier)
                    return;

                state.Identifier = identifier.ToIdentifier();

                _sawmill.Debug(JsonSerializer.Serialize(payload));

                var request = await _discord.CreateMessage(identifier.ToIdentifier(), payload);
                var content = await request.Content.ReadAsStringAsync();
                state.MessageId = ulong.Parse(JsonNode.Parse(content)?["id"]!.GetValue<string>()!);
            }
            catch (Exception e)
            {
                _sawmill.Error($"Error while sending vote webhook to Discord: {e}");
            }
        }

        // Edits a pre-existing payload message, given an ID
        private async void UpdateWebhookMessage(WebhookState state, WebhookPayload payload, ulong id)
        {
            if (state.MessageId == 0)
            {
                _sawmill.Warning("Failed to deliver update to custom vote webhook: message ID was zero. This likely indicates a previous connection error sending the original message.");
                return;
            }

            DebugTools.Assert(state.Identifier != default);

            try
            {
                await _discord.EditMessage(state.Identifier, id, payload);
            }
            catch (Exception e)
            {
                _sawmill.Error($"Error while updating vote webhook on Discord: {e}");
            }
        }

        private sealed class WebhookState
        {
            public required string WebhookUrl;
            public required WebhookPayload Payload;
            public WebhookIdentifier Identifier;
            public ulong MessageId;
        }
    }

    [AnyCommand]
    public sealed class VoteCommand : IConsoleCommand
    {
        public string Command => "vote";
        public string Description => Loc.GetString("cmd-vote-desc");
        public string Help => Loc.GetString("cmd-vote-help");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (shell.Player == null)
            {
                shell.WriteError(Loc.GetString("cmd-vote-on-execute-error-must-be-player"));
                return;
            }

            if (args.Length != 2)
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number-need-specific", ("properAmount", 2), ("currentAmount", args.Length)));
                return;
            }

            if (!int.TryParse(args[0], out var voteId))
            {
                shell.WriteError(Loc.GetString("cmd-vote-on-execute-error-invalid-vote-id"));
                return;
            }

            if (!int.TryParse(args[1], out var voteOption))
            {
                shell.WriteError(Loc.GetString("cmd-vote-on-execute-error-invalid-vote-options"));
                return;
            }

            var mgr = IoCManager.Resolve<IVoteManager>();
            if (!mgr.TryGetVote(voteId, out var vote))
            {
                shell.WriteError(Loc.GetString("cmd-vote-on-execute-error-invalid-vote"));
                return;
            }

            int? optionN;
            if (voteOption == -1)
            {
                optionN = null;
            }
            else if (vote.IsValidOption(voteOption))
            {
                optionN = voteOption;
            }
            else
            {
                shell.WriteError(Loc.GetString("cmd-vote-on-execute-error-invalid-option"));
                return;
            }

            vote.CastVote(shell.Player!, optionN);
        }
    }

    [AnyCommand]
    public sealed class ListVotesCommand : IConsoleCommand
    {
        public string Command => "listvotes";
        public string Description => Loc.GetString("cmd-listvotes-desc");
        public string Help => Loc.GetString("cmd-listvotes-help");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var mgr = IoCManager.Resolve<IVoteManager>();

            foreach (var vote in mgr.ActiveVotes)
            {
                shell.WriteLine($"[{vote.Id}] {vote.InitiatorText}: {vote.Title}");
            }
        }
    }

    [AdminCommand(AdminFlags.Moderator)]
    public sealed class CancelVoteCommand : IConsoleCommand
    {
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;

        public string Command => "cancelvote";
        public string Description => Loc.GetString("cmd-cancelvote-desc");
        public string Help => Loc.GetString("cmd-cancelvote-help");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var mgr = IoCManager.Resolve<IVoteManager>();

            if (args.Length < 1)
            {
                shell.WriteError(Loc.GetString("cmd-cancelvote-error-missing-vote-id"));
                return;
            }

            if (!int.TryParse(args[0], out var id) || !mgr.TryGetVote(id, out var vote))
            {
                shell.WriteError(Loc.GetString("cmd-cancelvote-error-invalid-vote-id"));
                return;
            }

            if (shell.Player != null)
                _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"{shell.Player} canceled vote: {vote.Title}");
            else
                _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"Canceled vote: {vote.Title}");
            vote.Cancel();
        }

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            var mgr = IoCManager.Resolve<IVoteManager>();
            if (args.Length == 1)
            {
                var options = mgr.ActiveVotes
                    .OrderBy(v => v.Id)
                    .Select(v => new CompletionOption(v.Id.ToString(), v.Title));

                return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-cancelvote-arg-id"));
            }

            return CompletionResult.Empty;
        }
    }
}
