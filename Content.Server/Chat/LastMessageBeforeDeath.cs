using System.Text;
using Content.Server.Discord;
using Robust.Shared.Timing;
using Content.Shared.Mobs.Systems;

namespace Content.Server.Chat
{
    //TODO: Remove all Console.Writeline's and get sawmill
    internal class LastMessageBeforeDeath
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        private readonly MobStateSystem _mobStateSystem;

        private Dictionary<EntityUid, Dictionary<string, string>> _playerData = new();

        private static LastMessageBeforeDeath? _instance;
        private static readonly object _lock = new object();

        // Private constructor to prevent instantiation
        private LastMessageBeforeDeath()
        {
            IoCManager.InjectDependencies(this);
            _mobStateSystem = IoCManager.Resolve<IEntityManager>().System<MobStateSystem>();
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

            message += $" | at [{_gameTiming.CurTime.ToString()}]";
            if (!_playerData.ContainsKey(player))
            {
                _playerData[player] = new Dictionary<string, string>();
            }

            // Replace the existing message with the new one
            _playerData[player][playerName] = message;

        }



        public async void OnRoundEnd(WebhookIdentifier? webhookId)
        {
            if (!webhookId.HasValue)
                return;

            // Now it's safe to access webhookId.Value
            var id = webhookId.Value;

            StringBuilder allMessagesString = new StringBuilder();
            foreach (var player in _playerData)
            {
                var playerUid = player.Key;
                if (_mobStateSystem.IsDead(playerUid))
                {
                    foreach (var playerName in player.Value)
                    {
                        allMessagesString.AppendLine($"{playerName.Key}: {playerName.Value}");
                    }
                }
            }
            if (allMessagesString.ToString().Length == 0)
            {
                allMessagesString.AppendLine($"No messages found.");
            }
            // Get this info from config later.

            var payload = new WebhookPayload
            {
                Content = allMessagesString.ToString()
            };
            var discordWebhook = new DiscordWebhook();
            var response = await discordWebhook.CreateMessage(id, payload);

            //if (response.IsSuccessStatusCode)
            //{
            //    _sawmill.Info("Last Messages Before Death sent successfully.");
            //}
            //else
            //{
            //    _sawmill.Warning($"Failed to send last messages before death. Status code: {response.StatusCode}");
            //}
        }
    }
}
