using System.Text.Json.Nodes;
using Content.Shared.CCVar;
using Robust.Server.ServerStatus;
using Robust.Shared.Configuration;

namespace Content.Server.ServerInfo;

/// <summary>
/// Adds additional data like info links to the server info endpoint
/// </summary>
public sealed class ServerInfoManager
{
    private static readonly (CVarDef<string> cVar, string icon, string name)[] Vars =
    {
        // @formatter:off
        (CCVars.InfoLinksDiscord,  "discord",  "info-link-discord"),
        (CCVars.InfoLinksForum,    "forum",    "info-link-forum"),
        (CCVars.InfoLinksGithub,   "github",   "info-link-github"),
        (CCVars.InfoLinksWebsite,  "web",      "info-link-website"),
        (CCVars.InfoLinksWiki,     "wiki",     "info-link-wiki"),
        (CCVars.InfoLinksTelegram, "telegram", "info-link-telegram")
        // @formatter:on
    };

    [Dependency] private readonly IStatusHost _statusHost = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ILocalizationManager _loc = default!;

    public void Initialize()
    {
        _statusHost.OnInfoRequest += OnInfoRequest;
    }

    private void OnInfoRequest(JsonNode json)
    {
        foreach (var (cVar, icon, name) in Vars)
        {
            var url = _cfg.GetCVar(cVar);
            if (string.IsNullOrEmpty(url))
                continue;

            StatusHostHelpers.AddLink(json, _loc.GetString(name), url, icon);
        }
    }
}
