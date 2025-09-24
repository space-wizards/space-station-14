using Content.Server.Administration;
using Content.Server.Medical.Disease.Cures;
using Content.Shared.Administration;
using Content.Shared.Medical.Disease;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Medical.Disease.Commands;

/// <summary>
/// Grants immunity to a disease to your attached entity.
/// </summary>
[AdminCommand(AdminFlags.Fun)]
public sealed class VaccinateCommand : LocalizedEntityCommands
{
    public override string Command => "vaccinate";
    public override string Description => Loc.GetString("cmd-vaccinate-desc");
    public override string Help => Loc.GetString("cmd-vaccinate-help");

    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IEntitySystemManager _sysMan = default!;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 1)
        {
            shell.WriteError(Loc.GetString("cmd-vaccinate-need-target"));
            shell.WriteLine(Help);
            return;
        }

        if (args.Length < 2)
        {
            shell.WriteError(Loc.GetString("cmd-vaccinate-need-id"));
            shell.WriteLine(Help);
            return;
        }

        if (!NetEntity.TryParse(args[0], out var net) || !EntityManager.TryGetEntity(net, out var resolved))
        {
            shell.WriteError(Loc.GetString("cmd-vaccinate-bad-target", ("value", args[0])));
            return;
        }

        var diseaseId = args[1];
        if (!_proto.HasIndex<DiseasePrototype>(diseaseId))
        {
            shell.WriteError(Loc.GetString("cmd-vaccinate-fail"));
            return;
        }

        var targetUid = resolved.Value;
        if (!_entMan.TryGetComponent(targetUid, out DiseaseCarrierComponent? comp))
            comp = _entMan.AddComponent<DiseaseCarrierComponent>(targetUid);

        var cureSystem = _sysMan.GetEntitySystem<DiseaseCureSystem>();
        if (_proto.TryIndex(diseaseId, out DiseasePrototype? disease))
        {
            if (comp.ActiveDiseases.TryGetValue(diseaseId, out var stageNum))
            {
                var stageSymptoms = Array.Empty<ProtoId<DiseaseSymptomPrototype>>();
                cureSystem.ApplyCureDisease((targetUid, comp), disease, stageSymptoms);
            }
        }
        shell.WriteLine(Loc.GetString("cmd-vaccinate-ok", ("target", targetUid.ToString()), ("disease", diseaseId)));
    }
}
