using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Content.Server.Discord;

public sealed partial class DiscordWebhook : IPostInjectInit
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };


    [GeneratedRegex(@"^https://discord\.com/api/webhooks/(\d+)/((?!.*/).*)$")]
    private static partial Regex DiscordRegex();

    [Dependency] private readonly ILogManager _log = default!;

    private const string BaseUrl = "https://discord.com/api/v10/webhooks";
    private readonly HttpClient _http = new();
    private ISawmill _sawmill = default!;

    // Max embed description length is 4096, according to https://discord.com/developers/docs/resources/channel#embed-object-embed-limits
    // Keep small margin, just to be safe
    public const ushort DescriptionMax = 4000;

    // Maximum length a message can be before it is cut off
    // Should be shorter than DescriptionMax
    public const ushort MessageLengthCap = 3000;

    // Text to be used to cut off messages that are too long. Should be shorter than MessageLengthCap
    public const string TooLongText = "... **(too long)**";


    private string GetUrl(WebhookIdentifier identifier)
    {
        return $"{BaseUrl}/{identifier.Id}/{identifier.Token}";
    }

    /// <summary>
    ///     Gets the webhook data from the given webhook url.
    /// </summary>
    /// <param name="url">The url to get the data from.</param>
    /// <returns>The webhook data returned from the url.</returns>
    public async Task<WebhookData?> GetWebhook(string url)
    {
        try
        {
            return await _http.GetFromJsonAsync<WebhookData>(url);
        }
        catch (Exception e)
        {
            _sawmill.Error($"Error getting discord webhook data.\n{e}");
            return null;
        }
    }

    /// <summary>
    ///     Gets the webhook data from the given webhook url.
    /// </summary>
    /// <param name="url">The url to get the data from.</param>
    /// <param name="onComplete">The delegate to invoke with the obtained data, if any.</param>
    public async void GetWebhook(string url, Action<WebhookData> onComplete)
    {
        if (await GetWebhook(url) is { } data)
            onComplete(data);
    }

    /// <summary>
    ///     Tries to get the webhook data from the given webhook url if it is not null or whitespace.
    /// </summary>
    /// <param name="url">The url to get the data from.</param>
    /// <param name="onComplete">The delegate to invoke with the obtained data, if any.</param>
    public async void TryGetWebhook(string url, Action<WebhookData> onComplete)
    {
        if (await GetWebhook(url) is { } data)
            onComplete(data);
    }

    public async Task<WebhookData?> GetWebhookData(string id, string token)
    {
        var response = await _http.GetAsync($"https://discord.com/api/v10/webhooks/{id}/{token}");

        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            _sawmill.Log(LogLevel.Error,
                $"Discord returned bad status code when trying to get webhook data (perhaps the webhook URL is invalid?): {response.StatusCode}\nResponse: {content}");
            return null;
        }

        return JsonSerializer.Deserialize<WebhookData>(content);
    }

    /// <summary>
    ///     Creates a new webhook message with the given identifier and payload.
    /// </summary>
    /// <param name="identifier">The identifier for the webhook url.</param>
    /// <param name="payload">The payload to create the message from.</param>
    /// <returns>The response from Discord's API.</returns>
    public async Task<HttpResponseMessage> CreateMessage(WebhookIdentifier identifier, WebhookPayload payload)
    {
        var url = $"{GetUrl(identifier)}?wait=true";
        var response = await _http.PostAsJsonAsync(url, payload, JsonOptions);

        LogResponse(response, "Create");

        return response;
    }

    /// <summary>
    ///     Deletes a webhook message with the given identifier and message id.
    /// </summary>
    /// <param name="identifier">The identifier for the webhook url.</param>
    /// <param name="messageId">The message id to delete.</param>
    /// <returns>The response from Discord's API.</returns>
    public async Task<HttpResponseMessage> DeleteMessage(WebhookIdentifier identifier, ulong messageId)
    {
        var url = $"{GetUrl(identifier)}/messages/{messageId}";
        var response = await _http.DeleteAsync(url);

        LogResponse(response, "Delete");

        return response;
    }

    /// <summary>
    ///     Creates a new webhook message with the given identifier, message id and payload.
    /// </summary>
    /// <param name="identifier">The identifier for the webhook url.</param>
    /// <param name="messageId">The message id to edit.</param>
    /// <param name="payload">The payload used to edit the message.</param>
    /// <returns>The response from Discord's API.</returns>
    public async Task<HttpResponseMessage> EditMessage(WebhookIdentifier identifier, ulong messageId, WebhookPayload payload)
    {
        var url = $"{GetUrl(identifier)}/messages/{messageId}";
        var response = await _http.PatchAsJsonAsync(url, payload, JsonOptions);

        LogResponse(response, "Edit");

        return response;
    }

    void IPostInjectInit.PostInject()
    {
        _sawmill = _log.GetSawmill("DISCORD");
    }

    /// <summary>
    ///     Logs detailed information about the HTTP response received from a Discord webhook request.
    ///     If the response status code is non-2XX it logs the status code, relevant rate limit headers.
    /// </summary>
    /// <param name="response">The HTTP response received from the Discord API.</param>
    /// <param name="methodName">The name (constant) of the method that initiated the webhook request (e.g., "Create", "Edit", "Delete").</param>
    private void LogResponse(HttpResponseMessage response, string methodName)
    {
        if (!response.IsSuccessStatusCode)
        {
            _sawmill.Error($"Failed to {methodName} message. Status code: {response.StatusCode}.");

            if (response.Headers.TryGetValues("Retry-After", out var retryAfter))
                _sawmill.Debug($"Failed webhook response Retry-After: {string.Join(", ", retryAfter)}");

            if (response.Headers.TryGetValues("X-RateLimit-Global", out var globalRateLimit))
                _sawmill.Debug($"Failed webhook response X-RateLimit-Global: {string.Join(", ", globalRateLimit)}");

            if (response.Headers.TryGetValues("X-RateLimit-Scope", out var rateLimitScope))
                _sawmill.Debug($"Failed webhook response X-RateLimit-Scope: {string.Join(", ", rateLimitScope)}");
        }
    }

    /// <summary>
    /// Retrieve the token and ID for the webhook at the given URL, and also sanity-check the URL.
    /// </summary>
    /// <param name="url">URL to get ID and token for.</param>
    /// <param name="webhookId">URL's webhook ID string.</param>
    /// <param name="webhookToken">URL's webhook token string.</param>
    /// <returns>Returns false if the URL does not appear to be valid, or if the webhook ID/token could not be retrieved.</returns>
    public bool TryGetWebhookIdToken(string url,
        [NotNullWhen(true)] out string? webhookId,
        [NotNullWhen(true)] out string? webhookToken)
    {
        webhookId = null;
        webhookToken = null;

        var match = DiscordRegex().Match(url);

        if (!match.Success)
        {
            // TODO: Ideally, CVar validation during setting should be better integrated
            _sawmill.Log(LogLevel.Warning, "Webhook URL does not appear to be valid.");
            return false;
        }

        if (match.Groups.Count <= 2)
        {
            _sawmill.Log(LogLevel.Error, "Could not get webhook ID or token.");
            return false;
        }

        webhookId = match.Groups[1].Value;
        webhookToken = match.Groups[2].Value;

        return true;
    }

    /// <summary>
    /// Attempts to post the payload to an existing message ID, and if none exists, creates a new one.
    /// If an error happens, the method returns false so that the relay interaction can be wiped.
    /// </summary>
    /// <param name="relayMessage">The DiscordRelayInteraction to check the ID of.</param>
    /// <param name="webhookUrl">The webhook's URL.</param>
    /// <param name="payload">The payload to be posted.</param>
    /// <returns>True if successfully posted/patched, false if an error occurred.</returns>
    public async Task<bool> PostPayload(DiscordRelayInteraction relayMessage, string webhookUrl, WebhookPayload payload)
    {
        // If no existing post exists, create a new one
        if (relayMessage.Id == null)
        {
            var request = await _http.PostAsync($"{webhookUrl}?wait=true",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

            var content = await request.Content.ReadAsStringAsync();
            if (!request.IsSuccessStatusCode)
            {
                _sawmill.Log(LogLevel.Error,
                    $"Discord returned bad status code when posting message (perhaps the message is too long?): {request.StatusCode}\nResponse: {content}");
                return false;
            }

            var id = JsonNode.Parse(content)?["id"];
            if (id == null)
            {
                _sawmill.Log(LogLevel.Error,
                    $"Could not find id in json-content returned from discord webhook: {content}");
                return false;
            }

            relayMessage.Id = id.ToString();
        }
        // Otherwise, look for an existing one and patch it.
        else
        {
            var request = await _http.PatchAsync($"{webhookUrl}/messages/{relayMessage.Id}",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

            if (!request.IsSuccessStatusCode)
            {
                var content = await request.Content.ReadAsStringAsync();
                _sawmill.Log(LogLevel.Error,
                    $"Discord returned bad status code when patching message (perhaps the message is too long?): {request.StatusCode}\nResponse: {content}");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Trim a message to be compliant with Discord message length cap.
    /// If too long, cut it off and add notice to the end.
    /// </summary>
    public string TrimMessage(string message)
    {
        if (message.Length > MessageLengthCap)
            return message[..(MessageLengthCap - TooLongText.Length)] + TooLongText;

        return message;
    }
}

/// <summary>
/// Base Discord relay interaction class, containing the ID and description of a webhook message.
/// Can be inherited to extend functionality and keep track of additional data.
/// </summary>
public abstract class DiscordRelayInteraction
{
    public string? Id;

    /// <summary>
    /// Contents for the discord message.
    /// </summary>
    public string Description = string.Empty;
}
