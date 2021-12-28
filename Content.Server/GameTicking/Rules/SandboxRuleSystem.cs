using Content.Server.Sandbox;
using Robust.Shared.IoC;

namespace Content.Server.GameTicking.Rules;

public class SandboxRuleSystem : GameRuleSystem
{
    [Dependency] private readonly ISandboxManager _sandbox = default!;

    public override string Prototype => "Sandbox";

    public override void Added()
    {
        _sandbox.IsSandboxEnabled = true;
    }

    public override void Removed()
    {
        _sandbox.IsSandboxEnabled = false;
    }
}
