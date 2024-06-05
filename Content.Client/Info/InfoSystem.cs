using Content.Client.Guidebook;
using Content.Shared.Info;
using Robust.Shared.Prototypes;

namespace Content.Client.Info;

public sealed class InfoSystem : EntitySystem
{
    public ProtoId<GuideEntryPrototype> Rules = "DefaultRuleset";

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
        Rules = message.Guide;
    }
}
