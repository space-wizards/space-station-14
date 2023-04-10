using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Medical.Wounds.Components;
using Content.Shared.Medical.Wounds.Systems;
using Robust.Shared.Console;

namespace Content.Server.Medical.Commands.Debug;

[AdminCommand(AdminFlags.Debug)]
public sealed class ListWounds : LocalizedCommands
{
    [Dependency] private IEntityManager _entityManager = default!;

    public override string Command { get; } = "GetAllWounds";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError("Incorrect arguments");
            return;
        }

        if (!int.TryParse(args[0], out var rawId))
        {
            shell.WriteError("Argument is not an number");
            return;
        }

        var target = new EntityUid(rawId);
        if (!target.IsValid())
        {
            shell.WriteError("EntityId Invalid");
            return;
        }

        PrintAllWoundData(target, shell);
    }
    private void PrintAllWoundData(EntityUid target, IConsoleShell shell)
    {
        if (!_entityManager.TrySystem(out SharedBodySystem? bodySystem) ||
            !_entityManager.TrySystem(out WoundSystem? woundSystem) ||
            !_entityManager.TryGetComponent(target, out BodyComponent? body))
            return;
        shell.WriteLine("=====================================================");
        shell.WriteLine($"Printing wounds for Entity:{target} {Identity.Name(target, _entityManager)}");

        var woundsFound = false;
        foreach (var (bodyPartId, bodyPart) in bodySystem.GetBodyChildren(target, body))
        {
            if (!woundSystem.TryGetAllWoundEntities(bodyPartId, out var woundIds) || woundIds.Count == 0)
                continue;
            shell.WriteLine("----------------------------------------------");
            shell.WriteLine($"BodyPart:{bodyPartId} {Identity.Name(bodyPartId, _entityManager)}");
            foreach (var woundId in woundIds)
            {
                var wound = _entityManager.GetComponent<WoundComponent>(woundId);
                shell.WriteLine($"Wound:{woundId} {Identity.Name(woundId, _entityManager)} | Severity {wound.Severity}");
            }
            woundsFound = true;
        }
        if (!woundsFound)
        {
            shell.WriteLine("No wounds found!");
        }
        shell.WriteLine("=====================================================");
    }
}
