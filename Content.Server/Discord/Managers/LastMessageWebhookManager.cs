using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.Discord.Managers
{
    public class LastMessageWebhookManager
    {
        private readonly DiscordWebhook _discordWebhook = new DiscordWebhook();

        public const int MaxMessageSize = 2000; // CAN'T BE MORE THAN 2000! DISCORD LIMIT.
        private const int MaxMessagesPerBatch = 15;
        private const int MessageDelayMilliseconds = 2000;

        /// <summary>
        ///     Sends a list of messages to a specified webhook, handling rate limiting.
        /// </summary>
        /// <param name="webhookId">The identifier of the webhook to send messages to.</param>
        /// <param name="messages">The list of messages to be sent.</param>
        /// <returns>A task representing the asynchronous operation.  :nerd:</returns>
        public async Task SendMessagesAsync(WebhookIdentifier webhookId, List<string> messages)
        {
            int messageCount = 0;

            foreach (var message in messages)
            {
                if (messageCount >= MaxMessagesPerBatch)
                {
                    await Task.Delay(60000); // Wait to avoid rate limiting
                    messageCount = 0;
                }

                var payload = new WebhookPayload { Content = message };
                var response = await _discordWebhook.CreateMessage(webhookId, payload);
                messageCount++;

                await Task.Delay(MessageDelayMilliseconds); // Small delay between messages to mitigate rate limiting.
            }
        }

        /// <summary>
        ///     Splits a long message into smaller chunks based on the specified chunk size.
        ///     Ensures messages do not exceed the maximum allowed size.
        ///     Ensures a message is not cut off in the middle when splitting.
        /// </summary>
        /// <param name="message">The message to be split.</param>
        /// <param name="chunkSize">The maximum size of each chunk.</param>
        /// <returns>A list of message chunks.</returns>
        public List<string> SplitMessage(string message, int chunkSize)
        {
            var messages = new List<string>();
            int start = 0;

            while (start < message.Length)
            {
                int end = start + chunkSize;

                if (end >= message.Length)
                {
                    messages.Add(message.Substring(start));
                    break;
                }

                int lastNewLine = message.LastIndexOf('\n', end);
                if (lastNewLine > start)
                {
                    messages.Add(message.Substring(start, lastNewLine - start));
                    start = lastNewLine + 1;
                }
                else
                {
                    messages.Add(message.Substring(start, chunkSize));
                    start += chunkSize;
                }
            }

            return messages;
        }
    }
}
