using System.Linq;
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

        var protoManager = IoCManager.Resolve<IPrototypeManager>();

        if (!protoManager.TryIndex<HTNCompoundTask>(args[0], out var compound))
        {
            shell.WriteError($"Unable to find HTN compound task for '{args[0]}'");
            return;
        }

        var htnSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<HTNSystem>();

        foreach (var line in htnSystem.GetDomain(compound).Split("\n"))
        {
            shell.WriteLine(line);
        }
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        var protoManager = IoCManager.Resolve<IPrototypeManager>();

        if (args.Length > 1)
            return CompletionResult.Empty;

        return CompletionResult.FromHintOptions(protoManager.EnumeratePrototypes<HTNCompoundTask>().Select(o => o.ID), "compound task");
    }
}
