using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Content.Server.Interfaces;
using Content.Server.Interfaces.Chat;
using Newtonsoft.Json;
using Robust.Server.Interfaces.ServerStatus;
using Robust.Server.ServerStatus;
using Robust.Shared.Asynchronous;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Content.Server
{
    internal sealed class MoMMILink : IMoMMILink, IPostInjectInit
    {
#pragma warning disable 649
        [Dependency] private readonly IConfigurationManager _configurationManager;
        [Dependency] private readonly IStatusHost _statusHost;
        [Dependency] private readonly IChatManager _chatManager;
        [Dependency] private readonly ITaskManager _taskManager;
#pragma warning restore 649

        private readonly HttpClient _httpClient = new HttpClient();

        void IPostInjectInit.PostInject()
        {
            _configurationManager.RegisterCVar<string>("status.mommiurl", null);
            _configurationManager.RegisterCVar<string>("status.mommipassword", null);

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
            var url = _configurationManager.GetCVar<string>("status.mommiurl");
            var password = _configurationManager.GetCVar<string>("status.mommipassword");
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

        private bool _handleChatPost(HttpMethod method, HttpListenerRequest request, HttpListenerResponse response)
        {
            if (method != HttpMethod.Post || request.Url.AbsolutePath != "/ooc")
            {
                return false;
            }

            var password = _configurationManager.GetCVar<string>("status.mommipassword");
            OOCPostMessage message;

            using (var streamReader = new StreamReader(request.InputStream, EncodingHelpers.UTF8))
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                var serializer = new JsonSerializer();
                try
                {
                    message = serializer.Deserialize<OOCPostMessage>(jsonReader);
                }
                catch (JsonSerializationException)
                {
                    response.Respond(method, "400 Bad Request", HttpStatusCode.BadRequest, "text/plain");
                    return true;
                }
            }

            if (message == null)
            {
                response.Respond(method, "400 Bad Request", HttpStatusCode.BadRequest, "text/plain");
                return true;
            }

            if (message.Password != password)
            {
                response.Respond(method, "Incorrect password", HttpStatusCode.Forbidden, "text/plain");
                return true;
            }

            _taskManager.RunOnMainThread(() => _chatManager.SendHookOOC(message.Sender, message.Contents));

            response.Respond(method, "Message received", HttpStatusCode.OK, "text/plain");

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
            [JsonProperty("password")] public string Password;

            [JsonProperty("sender")] public string Sender;

            [JsonProperty("contents")] public string Contents;
        }
    }
}
