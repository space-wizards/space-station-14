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
public sealed class NPCDomainCommand : IConsoleCommand
{
    [Dependency] private readonly IEntitySystemManager _sysManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    public string Command => "npcdomain";
    public string Description => Loc.GetString("cmd-npcdomain-desc");
    public string Help => Loc.GetString("cmd-npcdomain-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("shell-need-exactly-one-argument"));
            return;
        }

        if (!_protoManager.HasIndex<HTNCompoundPrototype>(args[0]))
        {
            shell.WriteError(Loc.GetString("cmd-npcdomain-unknown-task", ("task", args[0])));
            return;
        }

        var htnSystem = _sysManager.GetEntitySystem<HTNSystem>();

        foreach (var line in htnSystem.GetDomain(new HTNCompoundTask {Task = args[0]}).Split("\n"))
        {
            shell.WriteLine(line);
        }
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length > 1)
            return CompletionResult.Empty;

        return CompletionResult.FromHintOptions(CompletionHelper.PrototypeIDs<HTNCompoundPrototype>(proto: _protoManager), "compound task");
    }
}
