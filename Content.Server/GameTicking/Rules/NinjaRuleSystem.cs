using Content.Server.GameTicking.Rules.Components;
using Content.Server.Objectives;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Only handles round end text for ninja.
/// </summary>
public sealed class NinjaRuleSystem : GameRuleSystem<NinjaRuleComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NinjaRuleComponent, ObjectivesTextGetInfoEvent>(OnObjectivesTextGetInfo);
    }

    private void OnObjectivesTextGetInfo(EntityUid uid, NinjaRuleComponent comp, ref ObjectivesTextGetInfoEvent args)
    {
        args.Minds = comp.Minds;
        args.AgentName = Loc.GetString("ninja-round-end-agent-name");
    }
}
