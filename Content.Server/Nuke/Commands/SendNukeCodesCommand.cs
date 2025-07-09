using Content.Server.Administration;
using Content.Server.Station.Components;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Nuke.Commands;

[AdminCommand(AdminFlags.Fun)]
public sealed class SendNukeCodesCommand : LocalizedEntityCommands
{
    [Dependency] private readonly NukeCodePaperSystem _nukeCodeSystem = default!;

    public override string Command => "nukecodes";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("shell-need-exactly-one-argument"));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var uidNet) || !EntityManager.TryGetEntity(uidNet, out var uid))
        {
            shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
            return;
        }

        _nukeCodeSystem.SendNukeCodes(uid.Value);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length != 1)
            return CompletionResult.Empty;

        var stations = new List<CompletionOption>();
        var query = EntityManager.EntityQueryEnumerator<StationDataComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            var meta = EntityManager.GetComponent<MetaDataComponent>(uid);

            stations.Add(new CompletionOption(uid.ToString(), meta.EntityName));
        }

        return CompletionResult.FromHintOptions(stations, null);
    }
}
