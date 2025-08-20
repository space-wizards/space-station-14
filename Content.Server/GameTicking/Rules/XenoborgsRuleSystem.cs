using Content.Server.GameTicking.Rules.Components;
using Content.Shared.GameTicking.Components;

namespace Content.Server.GameTicking.Rules;

public sealed class XenoborgsRuleSystem : GameRuleSystem<XenoborgsRuleComponent>
{
    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void Started(EntityUid uid,
        XenoborgsRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {

    }
}
