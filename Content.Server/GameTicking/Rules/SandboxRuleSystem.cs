using Content.Server.GameTicking.Rules.Configurations;
using Content.Server.Sandbox;

namespace Content.Server.GameTicking.Rules;

public sealed class SandboxRuleSystem : GameRuleSystem
{
    [Dependency] private readonly SandboxSystem _sandbox = default!;

    public override string Prototype => "Sandbox";

    public override void Started()
    {
        _sandbox.IsSandboxEnabled = true;
    }

    public override void Ended()
    {
        _sandbox.IsSandboxEnabled = false;
    }
}
