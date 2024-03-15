using Content.Shared.Info;
using Robust.Shared.Log;

namespace Content.Client.Info;

public sealed class InfoSystem : EntitySystem
{
    public RulesMessage Rules = new RulesMessage("Server Rules", "The server did not send any rules.");
    [Dependency] private readonly RulesManager _rules = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<RulesMessage>(OnRulesReceived);
        Log.Debug("Requested server info.");
        RaiseNetworkEvent(new RequestRulesMessage());
    }

    private void OnRulesReceived(RulesMessage message, EntitySessionEventArgs eventArgs)
    {
        Log.Debug("Received server rules.");
        Rules = message;
        _rules.UpdateRules();
    }
}
