namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(WizardRuleSystem))]
public sealed partial class WizardRuleComponent : Component
{
    // TODO: Any rule related info like minds, Protos, Factions? Starting gear?
    //  Death Match has starting gear, thiefs and traitors don't in these comps
}
