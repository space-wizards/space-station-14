using Content.Server.GameTicking.Rules.Components;
using Content.Server.Sandbox;

namespace Content.Server.GameTicking.Rules;

public sealed partial class SandboxRuleSystem : GameRuleSystem<SandboxRuleComponent>
{
    [Dependency] private SandboxSystem _sandbox = default!;

    protected override void Started(EntityUid uid, SandboxRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
        _sandbox.IsSandboxEnabled = true;
    }

    protected override void Ended(EntityUid uid, SandboxRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);
        _sandbox.IsSandboxEnabled = false;
    }
}
