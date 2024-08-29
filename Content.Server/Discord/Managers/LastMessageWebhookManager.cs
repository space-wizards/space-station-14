using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.Discord.Managers
{
    public class LastMessageWebhookManager
    {
        [Dependency] private readonly DiscordWebhook _discordWebhook = default!;

        public const int MaxMessageSize = 2000; // CAN'T BE MORE THAN 2000! DISCORD LIMIT.
        private const int MaxMessagesPerBatch = 15;
        private const int MessageDelayMs = 2000;
        private const int RateLimitDelayMs = 60000;

        public void Initialize()
        {
            IoCManager.InjectDependencies(this);
        }

        /// <summary>
        ///     Sends a list of messages to a specified webhook, handling rate limiting.
        /// </summary>
        /// <param name="webhookId">The identifier of the webhook to send messages to.</param>
        /// <param name="messages">The list of messages to be sent.</param>
        /// <returns>A task representing the asynchronous operation.  :nerd:</returns>
        public async Task SendMessagesAsync(WebhookIdentifier? webhookId, List<string> messages)
        {
            if (_discordWebhook == null)
                return;

            if (webhookId == null || !webhookId.HasValue)
                return;

            var id = webhookId.Value;

            int messageCount = 0;

            foreach (var message in messages)
            {
                if (messageCount >= MaxMessagesPerBatch)
                {
                    await Task.Delay(RateLimitDelayMs); // Wait to avoid rate limiting
                    messageCount = 0;
                }

                var payload = new WebhookPayload { Content = message };
                var response = await _discordWebhook.CreateMessage(id, payload);
                if (!response.IsSuccessStatusCode)
                {
                    return; // Not sure if logging is needed here.
                }
                messageCount++;

                await Task.Delay(MessageDelayMs); // Small delay between messages to mitigate rate limiting.
            }
        }
    }
}
