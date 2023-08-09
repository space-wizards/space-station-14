using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Content.Server.Chat.Managers;
using Content.Shared.CCVar;
using Robust.Server.ServerStatus;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;

namespace Content.Server.MoMMI
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
            _statusHost.AddHandler(HandleChatPost);
        }

        public async void SendOOCMessage(string sender, string message)
        {
            var sentMessage = new MoMMIMessageOOC
            {
                Sender = sender,
                Contents = message
            };

            await SendMessageInternal("ooc", sentMessage);
        }

        private async Task SendMessageInternal(string type, object messageObject)
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

            var request = await _httpClient.PostAsJsonAsync(url, sentMessage);

            if (!request.IsSuccessStatusCode)
            {
                throw new Exception($"MoMMI returned bad status code: {request.StatusCode}");
            }
        }

        private async Task<bool> HandleChatPost(IStatusHandlerContext context)
        {
            if (context.RequestMethod != HttpMethod.Post || context.Url.AbsolutePath != "/ooc")
            {
                return false;
            }

            var password = _configurationManager.GetCVar(CCVars.StatusMoMMIPassword);

            if (string.IsNullOrEmpty(password))
            {
                await context.RespondErrorAsync(HttpStatusCode.Forbidden);
                return true;
            }

            OOCPostMessage? message = null;
            try
            {
                message = await context.RequestBodyJsonAsync<OOCPostMessage>();
            }
            catch (JsonException)
            {
                // message null so enters block down below.
            }

            if (message == null)
            {
                await context.RespondErrorAsync(HttpStatusCode.BadRequest);
                return true;
            }

            if (message.Password != password)
            {
                await context.RespondErrorAsync(HttpStatusCode.Forbidden);
                return true;
            }

            var sender = message.Sender;
            var contents = message.Contents.ReplaceLineEndings(" ");

            _taskManager.RunOnMainThread(() => _chatManager.SendHookOOC(sender, contents));

            await context.RespondAsync("Success", HttpStatusCode.OK);
            return true;
        }

        private sealed class MoMMIMessageBase
        {
            [JsonInclude] [JsonPropertyName("password")]
            public string Password = null!;

            [JsonInclude] [JsonPropertyName("type")]
            public string Type = null!;

            [JsonInclude] [JsonPropertyName("contents")]
            public object Contents = null!;
        }

        private sealed class MoMMIMessageOOC
        {
            [JsonInclude] [JsonPropertyName("sender")]
            public string Sender = null!;

            [JsonInclude] [JsonPropertyName("contents")]
            public string Contents = null!;
        }

        private sealed class OOCPostMessage
        {
#pragma warning disable CS0649
            [JsonInclude] [JsonPropertyName("password")]
            public string Password = null!;

            [JsonInclude] [JsonPropertyName("sender")]
            public string Sender = null!;

            [JsonInclude] [JsonPropertyName("contents")]
            public string Contents = null!;
#pragma warning restore CS0649
        }
    }
}
