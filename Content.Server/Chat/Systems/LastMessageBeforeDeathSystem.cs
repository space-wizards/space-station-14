using System.Text;
using Content.Server.Discord;
using Content.Shared.Mobs.Systems;
using System.Collections.Specialized;
using System.Collections;
using Content.Server.GameTicking;
using Content.Server.Database;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Content.Server.Discord.Managers;

namespace Content.Server.Chat.Systems
{
    internal class LastMessageBeforeDeathSystem
    {

        [Dependency] private readonly IServerDbManager _db = default!;

        private readonly MobStateSystem _mobStateSystem;
        private readonly GameTicker _gameTicker;
        private readonly IEntityManager _entManager;

        private readonly LastMessageWebhookManager _webhookManager = new LastMessageWebhookManager();
        private OrderedDictionary _playerData = new OrderedDictionary();

        private const int MaxICLength = 128;

        private static LastMessageBeforeDeathSystem? _instance;
        private static readonly object _lock = new object();

        private static readonly Random _random = new Random();

        private object? _userNetId;

        // Private constructor to prevent instantiation
        private LastMessageBeforeDeathSystem()
        {
            IoCManager.InjectDependencies(this);
            _mobStateSystem = IoCManager.Resolve<IEntityManager>().System<MobStateSystem>();
            _gameTicker = IoCManager.Resolve<IEntityManager>().System<GameTicker>();
            _entManager = IoCManager.Resolve<IEntityManager>();
        }

        // Public property to get the singleton instance
        public static LastMessageBeforeDeathSystem Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new LastMessageBeforeDeathSystem();
                    }
                    return _instance;
                }
            }
        }

        /// <summary>
        ///     Adds a message for a player to the internal data structure.
        ///     Truncates the message if it exceeds the maximum length.
        /// </summary>
        /// <param name="source">The entity UID of the message source.</param>
        /// <param name="player">The player's username.</param>
        /// <param name="message">The message content.</param>
        /// <param name="playerName">The name of the player's character.</param>
        public async void AddMessage(EntityUid source, string player, string message, string playerName)
        {

            var actorComponent = _entManager.GetComponentOrNull<ActorComponent>(source);
            if (actorComponent?.PlayerSession?.UserId != null)
            {
                _userNetId = actorComponent.PlayerSession.UserId;
            }
            else
            {
                _userNetId = null;
            }

            if (message.Length > MaxICLength)
            {
                // if the message is bigger than the message length limit, we make it cut off at a random iterval.
                // Example: Someone was giving a speech and got blown to bits.
                var randomLength = _random.Next(1, MaxICLength);
                message = message[..randomLength] + "-";
            }

            var currentTime = _gameTicker.RoundDuration();
            var truncatedTime = $"{currentTime.Hours:D2}:{currentTime.Minutes:D2}:{currentTime.Seconds:D2}";
            var formattedMessage = $"[{truncatedTime}] {playerName}: {message}";

            if (!_playerData.Contains(player))
            {
                _playerData[player] = new OrderedDictionary();
            }

            var playerMessages = _playerData[player] as OrderedDictionary ?? throw new InvalidOperationException("Player messages dictionary is not initialized.");
            playerMessages[playerName] = formattedMessage;
            playerMessages["EntityUid"] = source;
            playerMessages["NetUserId"] = _userNetId;
        }

        /// <summary>
        ///     Sends all collected messages through the webhook at the end of the round.
        ///     Skips messages from banned users.
        /// </summary>
        /// <param name="webhookId">The identifier of the webhook to send messages to.</param>
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
                        if (playerMessages["NetUserId"] is NetUserId userNetId)
                        {
                            var ban = await _db.GetServerBanAsync(null, userNetId, null);
                            if (ban != null)
                            {
                                continue; // Skip banned users
                            }
                        }
                        foreach (DictionaryEntry playerName in playerMessages)
                        {
                            if (playerName.Key.ToString() != "EntityUid" && playerName.Key.ToString() != "NetUserId")
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

            var messages = _webhookManager.SplitMessage(allMessagesString.ToString(), LastMessageWebhookManager.MaxMessageSize);
            await _webhookManager.SendMessagesAsync(id, messages);
        }
    }
}
