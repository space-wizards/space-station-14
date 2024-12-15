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
using Content.Shared.Weapons.Reflect;
using Content.Server.Administration.Logs.Converters;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;

namespace Content.Server.Chat.Systems
{
    internal class LastMessageBeforeDeathSystem : EntitySystem
    {
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly DiscordWebhook _discord = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
        [Dependency] private readonly ILogManager _logManager = default!;

        private bool _lastMessageWebhookEnabled = false;
        private int _maxICLengthCVar;
        private int _maxMessageSize;
        private int _maxMessagesPerBatch;
        private int _messageDelayMs;
        private int _rateLimitDelayMs;
        private WebhookIdentifier? _webhookIdentifierLastMessage;

        private readonly LastMessageWebhookManager _webhookManager = new LastMessageWebhookManager();

        private Dictionary<NetUserId, PlayerData> _playerData = new Dictionary<NetUserId, PlayerData>();

        private readonly Random _random = new Random();



        private class PlayerData
        {
            public Dictionary<MindComponent, CharacterData> Characters { get; } = new Dictionary<MindComponent, CharacterData>();
            public ICommonSession? PlayerSession { get; set; }
        }

        private class CharacterData
        {
            public string? LastMessage { get; set; } // Store only the last message
            public EntityUid EntityUid { get; set; }
            public TimeSpan MessageTime { get; set; } // Store the round time of the last message
        }

        public override void Initialize()
        {
            base.Initialize();
            Subs.CVar(_configManager, CCVars.DiscordLastMessageBeforeDeathWebhook, value =>
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _lastMessageWebhookEnabled = true;
                    _discord.GetWebhook(value, data => _webhookIdentifierLastMessage = data.ToIdentifier());
                }
            }, true);
            _maxICLengthCVar = _configManager.GetCVar(CCVars.DiscordLastMessageSystemMaxICLength);
            _maxMessageSize = _configManager.GetCVar(CCVars.DiscordLastMessageSystemMaxMessageLength);
            _maxMessagesPerBatch = _configManager.GetCVar(CCVars.DiscordLastMessageSystemMaxMessageBatch);
            _messageDelayMs = _configManager.GetCVar(CCVars.DiscordLastMessageSystemMessageDelay);
            _rateLimitDelayMs = _configManager.GetCVar(CCVars.DiscordLastMessageSystemMaxMessageBatchOverflowDelay);
        }
        /// <summary>
        ///     Adds a message to the character data for a given player session.
        /// </summary>
        /// <param name="source">The entity UID of the source.</param>
        /// <param name="playerSession">The player's current session.</param>
        /// <param name="message">The message to be added.</param>
        public async void AddMessage(EntityUid source, ICommonSession playerSession, string message)
        {
            if (!_lastMessageWebhookEnabled)
            {
                return;
            }

            if (!_playerData.ContainsKey(playerSession.UserId))
            {
                _playerData[playerSession.UserId] = new PlayerData();
                _playerData[playerSession.UserId].PlayerSession = playerSession;
            }
            var playerData = _playerData[playerSession.UserId];

            var mindContainerComponent = EntityManager.GetComponentOrNull<MindContainerComponent>(source);
            if (mindContainerComponent != null && mindContainerComponent.Mind != null)
            {
                var mindComponent = EntityManager.GetComponentOrNull<MindComponent>(mindContainerComponent.Mind.Value); // Get mind by EntityUID, well, I hope this is the correct way to do it.

                if (mindComponent != null && !playerData.Characters.ContainsKey(mindComponent))
                {
                    playerData.Characters[mindComponent] = new CharacterData();
                }
                if (mindComponent != null)
                {
                    var characterData = playerData.Characters[mindComponent];
                    characterData.LastMessage = message;
                    characterData.EntityUid = source;
                    characterData.MessageTime = _gameTicker.RoundDuration();
                }
            }
        }

        /// <summary>
        ///     Processes messages at the end of a round and sends them via webhook.
        /// </summary>
        public async void OnRoundEnd()
        {
            if (!_lastMessageWebhookEnabled)
                return;
            _webhookManager.Initialize();

            var allMessages = new List<string>();

            foreach (var player in _playerData)
            {
                var singlePlayerData = player.Value;
                if (player.Key != null && singlePlayerData.PlayerSession != null && singlePlayerData.PlayerSession.Status != SessionStatus.Disconnected)
                {
                    foreach (var character in singlePlayerData.Characters)
                    {
                        var characterData = character.Value;
                        // I am sure if there is a better way to go about checking if an EntityUID is no longer linked to an active entity...
                        // I don't know how tho...
                        if (_mobStateSystem.IsDead(characterData.EntityUid) || !EntityManager.TryGetComponent<MetaDataComponent>(characterData.EntityUid, out var metadata)) // Check if an entity is dead or doesn't exist
                        {
                            if (character.Key.CharacterName != null)
                            {
                                var message = await FormatMessage(characterData, character.Key.CharacterName);
                                allMessages.Add(message);
                            }
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
        /// <param name="characterName">The name of the character whose message is being formatted.</param>
        /// <returns>A formatted message string.</returns>
        private async Task<string> FormatMessage(CharacterData characterData, string characterName)
        {
            var message = characterData.LastMessage;
            if (message != null && message.Length > _maxICLengthCVar)
            {
                var randomLength = _random.Next(1, _maxICLengthCVar);
                message = message[..randomLength] + "-";
            }
            var messageTime = characterData.MessageTime;
            var truncatedTime = $"{messageTime.Hours:D2}:{messageTime.Minutes:D2}:{messageTime.Seconds:D2}";

            return $"[{truncatedTime}] {characterName}: {message}";
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
                if (concatenatedMessages.Length + message.Length + 1 > _maxMessageSize)
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

            await _webhookManager.SendMessagesAsync(_webhookIdentifierLastMessage, messagesToSend, _maxMessageSize, _maxMessagesPerBatch, _messageDelayMs, _rateLimitDelayMs);
        }
    }
}
