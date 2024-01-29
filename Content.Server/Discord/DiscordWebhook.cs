using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Content.Server.Discord;

public sealed class DiscordWebhook : IPostInjectInit
{
    [Dependency] private readonly ILogManager _log = default!;

    private const string BaseUrl = "https://discord.com/api/v10/webhooks";
    private readonly HttpClient _http = new();
    private ISawmill _sawmill = default!;

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
        catch
        {
            _sawmill.Error($"Error getting discord webhook data. Stack trace:\n{Environment.StackTrace}");
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

    /// <summary>
    ///     Creates a new webhook message with the given identifier and payload.
    /// </summary>
    /// <param name="identifier">The identifier for the webhook url.</param>
    /// <param name="payload">The payload to create the message from.</param>
    /// <returns>The response from Discord's API.</returns>
    public async Task<HttpResponseMessage> CreateMessage(WebhookIdentifier identifier, WebhookPayload payload)
    {
        var url = $"{GetUrl(identifier)}?wait=true";
        return await _http.PostAsJsonAsync(url, payload, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
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
        return await _http.DeleteAsync(url);
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
        return await _http.PatchAsJsonAsync(url, payload, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
    }

    void IPostInjectInit.PostInject()
    {
        _sawmill = _log.GetSawmill("DISCORD");
    }
}
