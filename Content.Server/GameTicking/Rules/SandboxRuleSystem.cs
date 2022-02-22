using Content.Server.Sandbox;
using Robust.Shared.IoC;

namespace Content.Server.GameTicking.Rules;

public sealed class SandboxRuleSystem : GameRuleSystem
{
    [Dependency] private readonly ISandboxManager _sandbox = default!;

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
