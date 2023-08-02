using System.Linq;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.Voting.Managers;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Voting;
using Robust.Server.Player;
using Robust.Shared.Console;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using System.Net.Http;
using System.Linq;

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

            if (shell.Player != null && !mgr.CanCallVote((IPlayerSession) shell.Player, type))
            {
                _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"{shell.Player} failed to start {type.ToString()} vote");
                shell.WriteError(Loc.GetString("cmd-createvote-cannot-call-vote-now"));
                return;
            }

            mgr.CreateStandardVote((IPlayerSession?) shell.Player, type);
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

    [AdminCommand(AdminFlags.Admin)]
    public sealed class CreateCustomCommand : IConsoleCommand
    {
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystem = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        private const int MaxArgCount = 10;

        public string Command => "customvote";
        public string Description => Loc.GetString("cmd-customvote-desc");
        public string Help => Loc.GetString("cmd-customvote-help");

        // Webhook stuff 
        private string _webhookUrl = _cfg.GetCVar(CCVars.DiscordVoteWebhook);;
        private string _serverName = _cfg.GetCVar(CVars.GameHostName);
        private string _avatarUrl = _cfg.GetCVar(CCVars.DiscordVoteAvatar);
        private readonly HttpClient _httpClient = new();
        private string webhookId = string.Empty;

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 3 || args.Length > MaxArgCount)
            {
                shell.WriteError(Loc.GetString("shell-need-between-arguments",("lower", 3), ("upper", 10)));
                return;
            }

            var title = args[0];

            var mgr = IoCManager.Resolve<IVoteManager>();

            var options = new VoteOptions
            {
                Title = title,
                Duration = TimeSpan.FromSeconds(30),
            };

            for (var i = 1; i < args.Length; i++)
            {
                options.Options.Add((args[i], i));
            }

            // Set up the webhook payload
            var _gameTicker = _entitySystem.GetEntitySystem<GameTicker>();

            var payload = new WebhookPayload()
            {
                AvatarUrl = _avatarUrl,
                Username = _serverName,
                Embeds = new List<Embed>
                {
                    new()
                    {
                        Title = $"{shell.Player}",
                        Color = 13438992, // 2353993
                        Description = options.Title,
                        Footer = new EmbedFooter
                        {
                            Text = $"{_serverName} {_gameTicker.RoundId} {_gameTicker.RunLevel}",
                        },

                        Fields = new List<Field> {},
                    },
                },
            };
            
            foreach (var voteOption in options.Options)
            {
                var NewVote = new Field() { Name = voteOption.text,  Value = "0"};
                payload.Embeds[0].Fields.Add(NewVote);
            }

            Console.Write(JsonSerializer.Serialize(payload));

            WebhookMessage(payload, webhookId);

            options.SetInitiatorOrServer((IPlayerSession?) shell.Player);

            if (shell.Player != null)
                _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"{shell.Player} initiated a custom vote: {options.Title} - {string.Join("; ", options.Options.Select(x => x.text))}");
            else
                _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"Initiated a custom vote: {options.Title} - {string.Join("; ", options.Options.Select(x => x.text))}");

            var vote = mgr.CreateVote(options);

            vote.OnFinished += (_, eventArgs) =>
            {
                var chatMgr = IoCManager.Resolve<IChatManager>();
                if (eventArgs.Winner == null)
                {
                    var ties = string.Join(", ", eventArgs.Winners.Select(c => args[(int) c]));
                    _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"Custom vote {options.Title} finished as tie: {ties}");
                    chatMgr.DispatchServerAnnouncement(Loc.GetString("cmd-customvote-on-finished-tie",("ties", ties)));
                }
                else
                {
                    _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"Custom vote {options.Title} finished: {args[(int) eventArgs.Winner]}");
                    chatMgr.DispatchServerAnnouncement(Loc.GetString("cmd-customvote-on-finished-win",("winner", args[(int) eventArgs.Winner])));
                }
            };
        }

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
                return CompletionResult.FromHint(Loc.GetString("cmd-customvote-arg-title"));

            if (args.Length > MaxArgCount)
                return CompletionResult.Empty;

            var n = args.Length - 1;
            return CompletionResult.FromHint(Loc.GetString("cmd-customvote-arg-option-n", ("n", n)));
        }

        // Sends the payload's message. 
        private async void WebhookMessage(WebhookPayload payload, string id)
        {
            if (id == string.Empty)
            {
                var request = await _httpClient.PostAsync($"{_webhookUrl}?wait=true",
                    new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

                var content = await request.Content.ReadAsStringAsync();
                webhookId = (string) JsonNode.Parse(content)?["id"]!;
            } 

            await _httpClient.PatchAsync($"{_webhookUrl}/messages/{id}",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
        }

        public struct WebhookPayload
        {
            [JsonPropertyName("username")]
            public string Username { get; set; } = "";

            [JsonPropertyName("avatar_url")]
            public string AvatarUrl { get; set; } = "";

            [JsonPropertyName("embeds")]
            public List<Embed>? Embeds { get; set; } = null;

            public WebhookPayload() { }
        }

        public struct Embed
        {
            [JsonPropertyName("title")]
            public string Title { get; set; } = "";

            [JsonPropertyName("description")]
            public string Description { get; set; } = "";

            [JsonPropertyName("color")]
            public int Color { get; set; } = 0;

            [JsonPropertyName("footer")]
            public EmbedFooter? Footer { get; set; } = null;

            [JsonPropertyName("fields")]
            public List<Field> Fields { get; set; } = default!;

            public Embed()
            {
            }
        }

        public struct EmbedFooter
        {
            [JsonPropertyName("text")]
            public string Text { get; set; } = "";

            public EmbedFooter()
            {
            }
        }

        public struct Field
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = "";

            [JsonPropertyName("value")]
            public string Value { get; set; } = "";

            [JsonPropertyName("inline")]
            public bool Inline { get; set; } = true;

            public Field()
            {
            }
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

            vote.CastVote((IPlayerSession) shell.Player!, optionN);
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

    [AdminCommand(AdminFlags.Admin)]
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
