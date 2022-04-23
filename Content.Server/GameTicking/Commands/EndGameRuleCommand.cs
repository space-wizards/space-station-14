using Content.Server.Administration;
using Content.Server.GameTicking.Rules;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Commands;

[AdminCommand(AdminFlags.Fun)]
public sealed class EndGameRuleCommand : IConsoleCommand
{
    [Dependency] private readonly IPrototypeManager _prot = default!;
    [Dependency] private readonly IEntitySystemManager _esm = default!;

    public string Command => "endgamerule";
    public string Description => "";
    public string Help => "endgamerule <rules>";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
            return;

        var gameTicker = _esm.GetEntitySystem<GameTicker>();

        foreach (var ruleId in args)
        {
            if (!_prot.TryIndex<GameRulePrototype>(ruleId, out var rule))
                continue;

            gameTicker.EndGameRule(rule);
        }
    }
}
