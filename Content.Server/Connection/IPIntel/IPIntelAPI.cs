using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace Content.Server.Connection.IPIntel;

public interface IIPIntelApi
{
    Task<HttpResponseMessage> GetIPScore(IPAddress ip);
}

public sealed class IPIntelApi : IIPIntelApi
{
    // Holds-The-HttpClient
    private readonly IHttpClientHolder _http;

    // CCvars
    private string? _contactEmail;
    private string? _baseUrl;
    private string? _flags;

    public IPIntelApi(
        IHttpClientHolder http,
        IConfigurationManager cfg)
    {
        _http = http;

        cfg.OnValueChanged(CCVars.GameIPIntelEmail, b => _contactEmail = b, true);
        cfg.OnValueChanged(CCVars.GameIPIntelBase, b => _baseUrl = b, true);
        cfg.OnValueChanged(CCVars.GameIPIntelFlags, b => _flags = b, true);
    }

    public Task<HttpResponseMessage> GetIPScore(IPAddress ip)
    {
        return _http.Client.GetAsync($"{_baseUrl}/check.php?ip={ip}&contact={_contactEmail}&flags={_flags}");
    }
}
