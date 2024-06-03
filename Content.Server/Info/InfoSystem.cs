using Content.Shared.CCVar;
using Content.Shared.Info;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;

namespace Content.Server.Info;

public sealed class InfoSystem : EntitySystem
{
    [Dependency] private readonly IResourceManager _res = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<RequestRulesMessage>(OnRequestRules);
    }

    private void OnRequestRules(RequestRulesMessage message, EntitySessionEventArgs eventArgs)
    {
        Log.Debug("Client requested rules.");
        var title = Loc.GetString(_cfg.GetCVar(CCVars.RulesHeader));
        var path = _cfg.GetCVar(CCVars.RulesFile);
        var rules = "Server could not read its rules.";
        try
        {
            rules = _res.ContentFileReadAllText($"/ServerInfo/{path}");
        }
        catch (Exception)
        {
            Log.Debug("Could not read server rules file.");
        }
        var response = new RulesMessage(title, rules);
        RaiseNetworkEvent(response, eventArgs.SenderSession.Channel);
    }
}
