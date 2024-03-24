using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Medical.Wounding.Components;
using Content.Shared.Medical.Wounding.Systems;
using Robust.Shared.Console;

namespace Content.Server.Medical.Commands.Debug;

[AdminCommand(AdminFlags.Debug)]
public sealed class PrintAllWounds : LocalizedCommands
{
    [Dependency] private IEntityManager _entityManager = default!;

    public override string Command { get; } = "PrintAllWounds";
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


        var target = _entityManager.GetEntity(new NetEntity(rawId));
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
        string output = "";

        output += "=====================================================\n";
        output += $"Printing wounds for Entity:{target} {Identity.Name(target, _entityManager)}\n";

        var woundsFound = false;
        foreach (var (bodyPartId, _) in bodySystem.GetBodyChildren(target, body))
        {
            if (!_entityManager.TryGetComponent<WoundableComponent>(bodyPartId, out var woundable))
                continue;
            output += "----------------------------------------------\n";
            output += $"BodyPart:{bodyPartId} {Identity.Name(bodyPartId, _entityManager)} HP:{woundable.Health} Int:{woundable.Integrity}\n";
            foreach (var wound in woundSystem.GetAllWounds(new (bodyPartId, woundable)))
            {
                output += $"WoundEntityId:{wound.Owner} {_entityManager.GetComponent<MetaDataComponent>(wound.Owner).EntityPrototype!.ID} | Severity {wound.Comp.Severity}\n";
            }
            woundsFound = true;
        }
        if (!woundsFound)
        {
            output += "No wounds found!\n";
        }
        output += "=====================================================\n";
        shell.WriteLine(output);
    }
}
