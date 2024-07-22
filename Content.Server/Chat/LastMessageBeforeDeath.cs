using System.Text;
using Content.Server.Discord;
using Robust.Shared.Timing;
using Content.Shared.Mobs.Systems;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.Collections;

namespace Content.Server.Chat
{
    internal class LastMessageBeforeDeath
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
#if DEBUG
        [Dependency] private readonly ILogManager _logManager = default!;
        private ISawmill _sawmill = default!;
#endif
        private readonly MobStateSystem _mobStateSystem;

        private OrderedDictionary _playerData = new OrderedDictionary();

        private static LastMessageBeforeDeath? _instance;
        private static readonly object _lock = new object();

        // Private constructor to prevent instantiation
        private LastMessageBeforeDeath()
        {
            IoCManager.InjectDependencies(this);
            _mobStateSystem = IoCManager.Resolve<IEntityManager>().System<MobStateSystem>();
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
        public async void AddMessage(string playerName, EntityUid player, string message)
        {
            if (_gameTiming == null)
            {
                throw new InvalidOperationException("GameTiming is not initialized.");
            }

            var currentTime = _gameTiming.CurTime;
            string truncatedTime = $"{currentTime.Hours:D2}:{currentTime.Minutes:D2}:{currentTime.Seconds:D2}";
            string formattedMessage = $"[{truncatedTime}] {playerName}: {message}";

            if (!_playerData.Contains(player))
            {
                _playerData[player] = new OrderedDictionary();
            }

            var playerMessages = _playerData[player] as OrderedDictionary ?? throw new InvalidOperationException("Player messages dictionary is not initialized.");
            playerMessages[playerName] = formattedMessage;
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
                var playerUid = (EntityUid)player.Key;
                if (_mobStateSystem.IsDead(playerUid))
                {
                    var playerMessages = player.Value as OrderedDictionary;
                    if (playerMessages != null)
                    {
                        foreach (DictionaryEntry playerName in playerMessages)
                        {
                            allMessagesString.AppendLine($"{playerName.Value}");
                        }
                    }
                }
            }

            if (allMessagesString.ToString().Length == 0)
            {
                allMessagesString.AppendLine($"No messages found.");
            }

            var messages = SplitMessage(allMessagesString.ToString(), 2000);
            var discordWebhook = new DiscordWebhook();
            int messageCount = 0;
            foreach (var message in messages)
            {
                if (messageCount >= 30)
                {
                    await Task.Delay(60000); // Wait for 1 minute
                    messageCount = 0;
                }
                var payload = new WebhookPayload { Content = message };
                var response = await discordWebhook.CreateMessage(id, payload);
                messageCount++;

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
            for (int i = 0; i < message.Length; i += chunkSize)
            {
                messages.Add(message.Substring(i, Math.Min(chunkSize, message.Length - i)));
            }
            return messages;
        }

    }
}
