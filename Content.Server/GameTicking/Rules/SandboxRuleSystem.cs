using Content.Server.GameTicking.Rules.Components;
using Content.Server.Sandbox;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking.Rules;

namespace Content.Server.GameTicking.Rules;

public sealed partial class SandboxRuleSystem : GameRuleSystem<SandboxRuleComponent>
{
    [Dependency] private SandboxSystem _sandbox = default!;

    protected override void Started(Entity<SandboxRuleComponent, GameRuleComponent> rule, ref GameRuleStartedEvent args)
    {
        base.Started(rule, ref args);
        _sandbox.IsSandboxEnabled = true;
    }

    protected override void Ended(Entity<SandboxRuleComponent> rule, ref GameRuleEndedEvent args)
    {
        base.Ended(rule, ref args);
        _sandbox.IsSandboxEnabled = false;
    }
}
