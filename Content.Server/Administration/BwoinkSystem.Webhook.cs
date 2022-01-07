#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Server.Administration
{
    public partial class BwoinkSystem : SharedBwoinkSystem
    {
        [Dependency] private readonly IConfigurationManager _config = default!;
        [Dependency] private readonly IPlayerLocator _playerLocator = default!;

        private readonly HttpClient _httpClient = new();
        private string _webhookUrl = string.Empty;
        private string _serverName = string.Empty;
        private readonly Dictionary<NetUserId, (string id, string username, string content)> _relayMessages = new();
        private readonly ConcurrentDictionary<NetUserId, int> _messageQueues = new();
        private readonly HashSet<NetUserId> _processingChannels = new();
        private const ushort MessageMax = 2000;
        private int _maxAdditionalChars;

        public void InitializeWebhook()
        {
            _config.OnValueChanged(CCVars.DiscordAHelpWebhook, OnWebhookChanged, true);
            _config.OnValueChanged(CVars.GameHostName, OnServerNameChanged, true);
            _maxAdditionalChars = GenerateAHelpMessage("", "", true, true).Length + Header("").Length;
        }

        private void OnServerNameChanged(string obj)
        {
            _serverName = obj;
        }

        public void ShutdownWebhook()
        {
            _config.UnsubValueChanged(CCVars.DiscordAHelpWebhook, OnWebhookChanged);
            _config.UnsubValueChanged(CVars.GameHostName, OnServerNameChanged);
        }

        private void OnWebhookChanged(string obj)
        {
            _webhookUrl = obj;
        }

        private string Header(string serverName) => $"Server: {serverName}";

        private async void ProcessQueue(NetUserId channelId, int since)
        {
            if (!_playerManager.TryGetSessionById(channelId, out var playerSession))
            {
                _sawmill.Log(LogLevel.Error, $"Unable to find player session for netuserid {channelId}");
                return;
            }

            var chLog = _history[channelId];
            if (!_relayMessages.TryGetValue(channelId, out var oldMessage) ||
                    chLog.GetRange(since, chLog.Count).Sum(
                        x => FormattedMessage.RemoveMarkup(x.Text).Length+2)
                            + oldMessage.content.Length > MessageMax)
            {
                var lookup = await _playerLocator.LookupIdAsync(channelId);

                if (lookup == null)
                {
                    _sawmill.Log(LogLevel.Error, $"Unable to find player for netuserid {channelId}.");
                    _relayMessages.Remove(channelId);
                    return;
                }

                oldMessage = (string.Empty, lookup.Username, Header(_serverName));
            }

            for (var i = since; i < chLog.Count; i++)
            {
                oldMessage.content += "\n" + GenerateAHelpMessage(
                        playerSession.Name,
                        FormattedMessage.RemoveMarkup(chLog[i].Text),
                        chLog[i].Status != SharedBwoinkSystem.Status.Delivered,
                        chLog[i].TrueSender != channelId
                );
            }

            var payload = new WebhookPayload()
            {
                username = oldMessage.username,
                content = oldMessage.content
            };

            if (oldMessage.id == string.Empty)
            {
                var request = await _httpClient.PostAsync($"{_webhookUrl}?wait=true",
                    new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

                var content = await request.Content.ReadAsStringAsync();
                if (!request.IsSuccessStatusCode)
                {
                    _sawmill.Log(LogLevel.Error, $"Discord returned bad status code when posting message: {request.StatusCode}\nResponse: {content}");
                    _relayMessages.Remove(channelId);
                    return;
                }

                var id = JsonNode.Parse(content)?["id"];
                if (id == null)
                {
                    _sawmill.Log(LogLevel.Error, $"Could not find id in json-content returned from discord webhook: {content}");
                    _relayMessages.Remove(channelId);
                    return;
                }

                oldMessage.id = id.ToString();
            }
            else
            {
                var request = await _httpClient.PatchAsync($"{_webhookUrl}/messages/{oldMessage.id}",
                    new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

                if (!request.IsSuccessStatusCode)
                {
                    var content = await request.Content.ReadAsStringAsync();
                    _sawmill.Log(LogLevel.Error, $"Discord returned bad status code when patching message: {request.StatusCode}\nResponse: {content}");
                    _relayMessages.Remove(channelId);
                    return;
                }
            }

            _relayMessages[channelId] = oldMessage;

            _messageQueues[channelId] = chLog.Count;
            _processingChannels.Remove(channelId);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var channelId in _messageQueues.Keys.ToArray())
            {
                if(_processingChannels.Contains(channelId))
                    continue;

                var since = _messageQueues[channelId];
                if (since >= _history[channelId].Count) // since should never be larger than the history list, but whatever
                    continue;

                _processingChannels.Add(channelId);

                ProcessQueue(channelId, since);
            }
        }

        private string GenerateAHelpMessage(string username, string message, bool delivered, bool admin)
        {
            var stringbuilder = new StringBuilder();
            if (delivered)
                stringbuilder.Append(":sos:");
            stringbuilder.Append(admin ? ":outbox_tray:" : ":inbox_tray:");
            stringbuilder.Append(' ');
            stringbuilder.Append(username);
            stringbuilder.Append(": ");
            stringbuilder.Append(message);
            return stringbuilder.ToString();
        }

        private struct WebhookPayload
        {
            // ReSharper disable once InconsistentNaming
            public string username { get; set; } = "";

            // ReSharper disable once InconsistentNaming
            public string content { get; set; } = "";

            // ReSharper disable once InconsistentNaming
            public Dictionary<string, string[]> allowed_mentions { get; set; } =
                new()
                {
                    { "parse", Array.Empty<string>() }
                };
        }
    }
}

