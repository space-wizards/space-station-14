using Content.Server.Speech.EntitySystems;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Random;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Fun)]
public sealed class OwoifyCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public override string Command => "owoify";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!int.TryParse(args[0], out var targetId))
        {
            shell.WriteLine(Loc.GetString("shell-argument-must-be-number"));
            return;
        }

        var nent = new NetEntity(targetId);

        if (!_entManager.TryGetEntity(nent, out var eUid))
        {
            return;
        }

        var meta = _entManager.GetComponent<MetaDataComponent>(eUid.Value);

        var owoSys = _entManager.System<OwOAccentSystem>();
        var metaDataSys = _entManager.System<MetaDataSystem>();

        metaDataSys.SetEntityName(eUid.Value, owoSys.Accentuate(meta.EntityName), meta);
        metaDataSys.SetEntityDescription(eUid.Value, owoSys.Accentuate(meta.EntityDescription), meta);
    }
}
