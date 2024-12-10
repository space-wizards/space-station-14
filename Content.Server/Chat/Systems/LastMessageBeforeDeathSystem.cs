using System.Text;
using Content.Server.Discord;
using Content.Shared.Mobs.Systems;
using System.Collections.Generic;
using Content.Server.GameTicking;
using Content.Server.Database;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Content.Server.Discord.Managers;
using Robust.Shared.Configuration;
using System.Threading.Tasks;
using Content.Shared.CCVar;
using Robust.Shared.Enums;

namespace Content.Server.Chat.Systems
{
    internal class LastMessageBeforeDeathSystem : EntitySystem
    {
        [Dependency] private readonly IServerDbManager _db = default!;
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly ILogManager _logManager = default!;
        [Dependency] private readonly DiscordWebhook _discord = default!;

        private MobStateSystem _mobStateSystem = default!;
        private GameTicker _gameTicker = default!;
        private IEntityManager _entManager = default!;

        private ISawmill _sawmill = default!;

        private readonly LastMessageWebhookManager _webhookManager = new LastMessageWebhookManager();
        private Dictionary<ICommonSession, PlayerData> _playerData = new Dictionary<ICommonSession, PlayerData>();

        private const string MaxICLengthCVar = "chat.max_ic_length";
        private const int MaxWebhookMessageLength = 2000; // This should not really be changed... since the maximum characters per message for webhooks is 2000.

        private static readonly Random _random = new Random();

        private WebhookIdentifier? _webhookIdentifierLastMessage;

        private class PlayerData
        {
            public Dictionary<string, CharacterData> Characters { get; set; } = new Dictionary<string, CharacterData>();
            public NetUserId? NetUserId { get; set; }
            public ICommonSession? PlayerSession { get; set; }
        }

        private class CharacterData
        {
            public string? Message { get; set; } // Store only the last message
            public EntityUid EntityUid { get; set; }
            public string? CharacterName { get; set; }
            public TimeSpan MessageTime { get; set; } // Store the round time of the last message
        }

        public override void Initialize()
        {
            base.Initialize();
            IoCManager.InjectDependencies(this);
            Subs.CVar(_configManager, CCVars.DiscordLastMessageBeforeDeathWebhook, value =>
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _discord.GetWebhook(value, data => _webhookIdentifierLastMessage = data.ToIdentifier());
                }
            }, true);
            _mobStateSystem = _entityManager.System<MobStateSystem>();
            _gameTicker = _entityManager.System<GameTicker>();
            _entManager = _entityManager;
            _sawmill = _logManager.GetSawmill("lastmessagebeforedeath");
        }

        /// <summary>
        ///     Adds a message to the character data for a given player session.
        /// </summary>
        /// <param name="source">The entity UID of the source.</param>
        /// <param name="playerSession">The session of the player sending the message.</param>
        /// <param name="message">The message to be added.</param>
        /// <param name="characterName">The name of the character sending the message.</param>
        public async void AddMessage(EntityUid source, ICommonSession playerSession, string message, string characterName)
        {

            if (!_playerData.ContainsKey(playerSession))
            {
                _playerData[playerSession] = new PlayerData();
            }

            var playerData = _playerData[playerSession];
            if (!playerData.Characters.ContainsKey(characterName))
            {
                playerData.Characters[characterName] = new CharacterData();
                playerData.PlayerSession = playerSession;
            }

            var characterData = playerData.Characters[characterName];
            characterData.Message = message; 
            characterData.EntityUid = source;
            characterData.CharacterName = characterName;
            characterData.MessageTime = _gameTicker.RoundDuration(); 
        }

        /// <summary>
        ///     Processes messages at the end of a round and sends them via webhook.
        /// </summary>
        public async void OnRoundEnd()
        {
            if (_webhookIdentifierLastMessage == null || !_webhookIdentifierLastMessage.HasValue)
                return;

            _sawmill.Info("Last Message Before Death Webhook is processing messages...");
            _webhookManager.Initialize();

            var allMessages = new List<string>();

            foreach (var player in _playerData)
            {
                var playerData = player.Value;
                if (playerData.PlayerSession != null && playerData.PlayerSession.Status != SessionStatus.Disconnected)
                {
                    foreach (var character in playerData.Characters)
                    {
                        var characterData = character.Value;
                        // I am sure if there is a better way to go about checking if an EntityUID is no longer linked to an active entity...
                        // I don't know how tho...
                        if (_mobStateSystem.IsDead(characterData.EntityUid) || !_entManager.TryGetComponent<MetaDataComponent>(characterData.EntityUid, out var metadata)) // Check if an entity is dead or doesn't exist
                        {
                            var message = await FormatMessage(characterData);
                            allMessages.Add(message);
                        }
                    }
                }
            }

            await SendMessagesInBatches(allMessages);

            // Clear all stored data upon round restart
            _playerData.Clear();
        }

        /// <summary>
        ///     Formats a message for the "last message before death" system.
        /// </summary>
        /// <param name="characterData">The data of the character whose message is being formatted.</param>
        /// <returns>A formatted message string.</returns>
        private async Task<string> FormatMessage(CharacterData characterData)
        {
            _sawmill.Info("Formatting message for last message before death system.");

            var message = characterData.Message;
            var maxICLength = _configManager.GetCVar<int>(MaxICLengthCVar);
            if (message != null && message.Length > maxICLength)
            {
                var randomLength = _random.Next(1, maxICLength);
                message = message[..randomLength] + "-";
            }
            var messageTime = characterData.MessageTime;
            var truncatedTime = $"{messageTime.Hours:D2}:{messageTime.Minutes:D2}:{messageTime.Seconds:D2}";

            return $"[{truncatedTime}] {characterData.CharacterName}: {message}";
        }

        /// <summary>
        ///     Sends messages in batches via webhook.
        /// </summary>
        /// <param name="messages">The list of messages to be sent.</param>
        private async Task SendMessagesInBatches(List<string> messages)
        {
            var concatenatedMessages = new StringBuilder();
            var messagesToSend = new List<string>();

            foreach (var message in messages)
            {
                if (concatenatedMessages.Length + message.Length + 1 > MaxWebhookMessageLength)
                {
                    messagesToSend.Add(concatenatedMessages.ToString());
                    concatenatedMessages.Clear();
                }
                concatenatedMessages.AppendLine(message);
            }

            if (concatenatedMessages.Length > 0)
            {
                messagesToSend.Add(concatenatedMessages.ToString());
            }

            await _webhookManager.SendMessagesAsync(_webhookIdentifierLastMessage, messagesToSend);
        }
    }
}
