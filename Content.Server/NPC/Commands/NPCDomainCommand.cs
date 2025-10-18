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
    public string Description => "Lists the domain of a particular HTN compound task";
    public string Help => $"{Command} <htncompoundtask>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
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
