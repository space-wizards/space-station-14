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
