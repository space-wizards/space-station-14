using System.Text;
using Content.Server.Discord;
using Content.Shared.Mobs.Systems;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.Collections;
using Content.Server.GameTicking;
using Content.Server.Administration.Managers;

namespace Content.Server.Chat
{
    internal class LastMessageBeforeDeath
    {

#if DEBUG
        [Dependency] private readonly ILogManager _logManager = default!;
        private ISawmill _sawmill = default!;
#endif
        [Dependency] private readonly IBanManager _banManager = default!;

        private readonly MobStateSystem _mobStateSystem;
        private readonly GameTicker _gameTicker;


        private OrderedDictionary _playerData = new OrderedDictionary();

        // I don't think Chat needs it's own CVar file because of these two, so I'll leave them here...
        private const int MessageDelayMilliseconds = 2000;
        private const int MaxMessageSize = 2000; // CAN'T BE MORE THAN 2000! DISCORD LIMIT.
        private const int MaxMessagesPerBatch = 15;
        private const int MaxICLength = 128;

        private static LastMessageBeforeDeath? _instance;
        private static readonly object _lock = new object();

        private static readonly Random _random = new Random();

        // Private constructor to prevent instantiation
        private LastMessageBeforeDeath()
        {
            IoCManager.InjectDependencies(this);
            _mobStateSystem = IoCManager.Resolve<IEntityManager>().System<MobStateSystem>();
            _gameTicker = IoCManager.Resolve<IEntityManager>().System<GameTicker>();
            _banManager.ServerBanCreated += OnServerBanCreated;
#if DEBUG
            _sawmill = _logManager.GetSawmill("lastDeathMsgWebhook");
#endif
        }

        // Public property to get the singleton instance
        public static LastMessageBeforeDeath Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new LastMessageBeforeDeath();
                    }
                    return _instance;
                }
            }
        }

        // Method to add a message for a player
        public async void AddMessage(EntityUid source, string player, string message, string playerName)
        {
            if (message.Length > MaxICLength)
            {
                // if the message is bigger than the message length limit, we make it cut off at a random iterval.
                // Example: Someone was giving a speech and got blown to bits.
                int randomLength = _random.Next(1, MaxICLength);
                message = message[..randomLength] + "-";
            }

            var currentTime = _gameTicker.RoundDuration();
            string truncatedTime = $"{currentTime.Hours:D2}:{currentTime.Minutes:D2}:{currentTime.Seconds:D2}";
            string formattedMessage = $"[{truncatedTime}] {playerName}: {message}";

            if (!_playerData.Contains(player))
            {
                _playerData[player] = new OrderedDictionary();
            }

            var playerMessages = _playerData[player] as OrderedDictionary ?? throw new InvalidOperationException("Player messages dictionary is not initialized.");
            playerMessages[playerName] = formattedMessage;
            playerMessages["EntityUid"] = source;
        }

        // Send messages through the webhook on round end
        public async void OnRoundEnd(WebhookIdentifier? webhookId)
        {
            if (!webhookId.HasValue)
                return;

            var id = webhookId.Value;
            StringBuilder allMessagesString = new StringBuilder();
            foreach (DictionaryEntry player in _playerData)
            {
                var playerMessages = player.Value as OrderedDictionary;
                if (playerMessages != null)
                {
                    if (playerMessages["EntityUid"] is EntityUid playerUid && _mobStateSystem.IsDead(playerUid))
                    {
                        foreach (DictionaryEntry playerName in playerMessages)
                        {
                            if (playerName.Key.ToString() != "EntityUid")
                            {
                                allMessagesString.AppendLine($"{playerName.Value}");
                            }
                        }
                    }
                }
            }

            if (allMessagesString.ToString().Length == 0)
            {
                allMessagesString.AppendLine($"No messages found.");
            }

            var messages = SplitMessage(allMessagesString.ToString(), MaxMessageSize);
            var discordWebhook = new DiscordWebhook();
            int messageCount = 0;
            foreach (var message in messages)
            {
                if (messageCount >= MaxMessagesPerBatch)
                {
                    await Task.Delay(60000); // Wait for 1 minute
                    messageCount = 0;
                }
                var payload = new WebhookPayload { Content = message };
                var response = await discordWebhook.CreateMessage(id, payload);
                messageCount++;

                // Insert a small delay between each message
                await Task.Delay(MessageDelayMilliseconds);

                // Response still can be handled if needed.
#if DEBUG
                if (response.IsSuccessStatusCode)
                {
                    _sawmill.Debug("Last Messages Before Death sent successfully.");
                }
                else
                {
                    _sawmill.Error($"Failed to send last messages before death. Status code: {response.StatusCode}");
                }
#endif
            }
        }



        private List<string> SplitMessage(string message, int chunkSize)
        {
            var messages = new List<string>();
            int start = 0;

            while (start < message.Length)
            {
                int end = start + chunkSize;

                if (end >= message.Length)
                {
                    messages.Add(message.Substring(start));
                    break;
                }

                int lastNewLine = message.LastIndexOf('\n', end);
                if (lastNewLine > start)
                {
                    messages.Add(message.Substring(start, lastNewLine - start));
                    start = lastNewLine + 1;
                }
                else
                {
                    messages.Add(message.Substring(start, chunkSize));
                    start += chunkSize;
                }
            }

            return messages;
        }

        private void OnServerBanCreated(object? sender, BanEventArgs.ServerBanEventArgs e)
        {
            // Handle the event
            if (e.TargetUsername != null && _playerData.Contains(e.TargetUsername))
            {
                _playerData.Remove(e.TargetUsername);
#if DEBUG
                _sawmill.Info("A user was removed from last message list due to a ban.");
#endif
            }
        }

    }
}
