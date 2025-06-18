using Content.Server.Administration;
using Content.Server.NPC.HTN;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.NPC.Commands;

/// <summary>
/// Lists out the domain of a particular HTN compound task.
/// </summary>
[AdminCommand(AdminFlags.Debug)]
public sealed class NpcDomainCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly HTNSystem _htnSystem = default!;

    public override string Command => "npcdomain";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError("shell-need-exactly-one-argument");
            return;
        }

        if (!_protoManager.HasIndex<HTNCompoundPrototype>(args[0]))
        {
            shell.WriteError($"Unable to find HTN compound task for '{args[0]}'");
            return;
        }

        foreach (var line in _htnSystem.GetDomain(new HTNCompoundTask {Task = args[0]}).Split("\n"))
        {
            shell.WriteLine(line);
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length > 1)
            return CompletionResult.Empty;

        return CompletionResult.FromHintOptions(CompletionHelper.PrototypeIDs<HTNCompoundPrototype>(proto: _protoManager),
            Loc.GetString($"cmd-npcdomain-hint"));
    }
}
