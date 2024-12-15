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

        public void Initialize()
        {
            IoCManager.InjectDependencies(this);
        }

        /// <summary>
        ///     Sends a list of messages to a specified webhook, handling rate limiting.
        /// </summary>
        /// <param name="webhookId">The identifier of the webhook to send messages to.</param>
        /// <param name="messages">The list of messages to be sent.</param>
        /// <param name="MaxMessageSize">The maximum message size in characters.</param>
        /// <param name="MaxMessagesPerBatch">The maximum amount of messages the webhook can send before the RateLimitDelayMs is used.</param>
        /// <param name="MessageDelayMs">Delay between each message in ms.</param>
        /// <param name="RateLimitDelayMs">Delay (in ms) after MaxMessagesPerBatch is exceeded.</param>
        /// <returns>A task representing the asynchronous operation.  :nerd:</returns>
        public async Task SendMessagesAsync(WebhookIdentifier? webhookId, List<string> messages, int MaxMessageSize, int MaxMessagesPerBatch, int MessageDelayMs, int RateLimitDelayMs)
        {
            if (_discordWebhook == null)
                return;

            if (MaxMessageSize > 2000)
            {
                throw new ArgumentOutOfRangeException("A discord webhook message can't contain more than 2000 characters.");
            }

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
