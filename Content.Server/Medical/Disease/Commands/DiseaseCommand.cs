using System.Globalization;
using Content.Server.Medical.Disease.Systems;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Medical.Disease.Commands;

/// <summary>
/// Infects your attached entity with a disease at an optional stage.
/// </summary>
[AdminCommand(AdminFlags.Fun)]
public sealed class InfectCommand : LocalizedEntityCommands
{
    public override string Command => "infect";
    public override string Description => Loc.GetString("cmd-infect-desc");
    public override string Help => Loc.GetString("cmd-infect-help");

    [Dependency] private readonly IEntitySystemManager _sysMan = default!;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 1)
        {
            shell.WriteError(Loc.GetString("cmd-infect-need-id"));
            shell.WriteLine(Help);
            return;
        }

        if (args.Length < 2)
        {
            shell.WriteError(Loc.GetString("cmd-infect-need-target"));
            shell.WriteLine(Help);
            return;
        }

        if (!NetEntity.TryParse(args[0], out var parsedNet) || !EntityManager.TryGetEntity(parsedNet, out var parsedUid))
        {
            shell.WriteError(Loc.GetString("cmd-infect-bad-target", ("value", args[0])));
            return;
        }

        var disease = _sysMan.GetEntitySystem<DiseaseSystem>();
        var targetUid = parsedUid.Value;
        var diseaseId = args[1];

        // Optional stage as 3rd argument.
        var stage = 1;
        if (args.Length >= 3 && int.TryParse(args[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedStage))
            stage = Math.Max(1, parsedStage);

        if (!disease.Infect(targetUid, diseaseId, stage))
        {
            shell.WriteError(Loc.GetString("cmd-infect-fail"));
            return;
        }

        shell.WriteLine(Loc.GetString("cmd-infect-ok", ("target", targetUid.ToString()), ("disease", diseaseId), ("stage", stage)));
    }
}
