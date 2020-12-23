using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Content.Server.Interfaces;
using Content.Server.Interfaces.Chat;
using Content.Shared;
using Newtonsoft.Json;
using Robust.Server.Interfaces.ServerStatus;
using Robust.Shared.Asynchronous;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Content.Server
{
    internal sealed class MoMMILink : IMoMMILink, IPostInjectInit
    {
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly IStatusHost _statusHost = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly ITaskManager _taskManager = default!;

        private readonly HttpClient _httpClient = new();

        void IPostInjectInit.PostInject()
        {
            _statusHost.AddHandler(_handleChatPost);
        }

        public async void SendOOCMessage(string sender, string message)
        {
            var sentMessage = new MoMMIMessageOOC
            {
                Sender = sender,
                Contents = message
            };

            await _sendMessageInternal("ooc", sentMessage);
        }

        private async Task _sendMessageInternal(string type, object messageObject)
        {
            var url = _configurationManager.GetCVar(CCVars.StatusMoMMIUrl);
            var password = _configurationManager.GetCVar(CCVars.StatusMoMMIPassword);
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                Logger.WarningS("mommi", "MoMMI URL specified but not password!");
                return;
            }

            var sentMessage = new MoMMIMessageBase
            {
                Password = password,
                Type = type,
                Contents = messageObject
            };

            var jsonMessage = JsonConvert.SerializeObject(sentMessage);
            var request =
                await _httpClient.PostAsync(url, new StringContent(jsonMessage, Encoding.UTF8, "application/json"));

            if (!request.IsSuccessStatusCode)
            {
                throw new Exception($"MoMMI returned bad status code: {request.StatusCode}");
            }
        }

        private bool _handleChatPost(IStatusHandlerContext context)
        {
            if (context.RequestMethod != HttpMethod.Post || context.Url!.AbsolutePath != "/ooc")
            {
                return false;
            }

            var password = _configurationManager.GetCVar(CCVars.StatusMoMMIPassword);

            if (string.IsNullOrEmpty(password))
            {
                context.RespondError(HttpStatusCode.Forbidden);
                return true;
            }

            OOCPostMessage message = null;
            try
            {
                message = context.RequestBodyJson<OOCPostMessage>();
            }
            catch (JsonSerializationException)
            {
                // message null so enters block down below.
            }

            if (message == null)
            {
                context.RespondError(HttpStatusCode.BadRequest);
                return true;
            }

            if (message.Password != password)
            {
                context.RespondError(HttpStatusCode.Forbidden);
                return true;
            }

            _taskManager.RunOnMainThread(() => _chatManager.SendHookOOC(message.Sender, message.Contents));

            context.Respond("Success", HttpStatusCode.OK);

            return false;
        }

        [JsonObject(MemberSerialization.Fields)]
        private class MoMMIMessageBase
        {
            [JsonProperty("password")] public string Password;

            [JsonProperty("type")] public string Type;

            [JsonProperty("contents")] public object Contents;
        }

        [JsonObject(MemberSerialization.Fields)]
        private class MoMMIMessageOOC
        {
            [JsonProperty("sender")] public string Sender;

            [JsonProperty("contents")] public string Contents;
        }

        [JsonObject(MemberSerialization.Fields, ItemRequired = Required.Always)]
        private class OOCPostMessage
        {
            #pragma warning disable CS0649
            [JsonProperty("password")] public string Password;

            [JsonProperty("sender")] public string Sender;

            [JsonProperty("contents")] public string Contents;
            #pragma warning restore CS0649
        }
    }
}
