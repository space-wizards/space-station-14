using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace Content.Server.Corvax.Sponsors;

public sealed class SponsorsManager : ISponsorsManager
{
    [Dependency] private readonly IServerNetManager _netMgr = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private readonly HttpClient _httpClient = new();

    private ISawmill _sawmill = default!;
    private string _apiUrl = string.Empty;

    private readonly Dictionary<NetUserId, string?> _cachedOOCColors = new();

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("sponsors");
        _cfg.OnValueChanged(CCVars.SponsorsApiUrl, s => _apiUrl = s, true);
        _netMgr.Connecting += OnConnecting;
    }

    public bool TryGetCustomOOCColor(NetUserId userId, [MaybeNullWhen(false)] out string color)
    {
        return _cachedOOCColors.TryGetValue(userId, out color);
    }

    private async Task OnConnecting(NetConnectingArgs e)
    {
        var info = await GetSponsorInfo(e.UserId);
        var isSponsor = info?.Tier != null;
        if (!isSponsor)
            return;

        var hexColor = info?.OOCColor != null ? $"#{info?.OOCColor}" : null;
        _cachedOOCColors[e.UserId] = hexColor;
    }

    private async Task<SponsorInfoResponse?> GetSponsorInfo(NetUserId userId)
    {
        if (string.IsNullOrEmpty(_apiUrl))
            return null;

        var url = $"{_apiUrl}/sponsors/{userId.ToString()}";
        var response = await _httpClient.GetAsync(url);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        if (response.StatusCode != HttpStatusCode.OK)
        {
            var errorText = await response.Content.ReadAsStringAsync();
            _sawmill.Error(
                "Failed to get player sponsor OOC color from API: [{StatusCode}] {Response}",
                response.StatusCode,
                errorText);
            return null;
        }

        return await response.Content.ReadFromJsonAsync<SponsorInfoResponse>();
    }
    

    private struct SponsorInfoResponse
    {
        [JsonPropertyName("sponsor_tier")]
        public int? Tier { get; set; }

        [JsonPropertyName("ooc_color")]
        public string? OOCColor { get; set; }
    }
}