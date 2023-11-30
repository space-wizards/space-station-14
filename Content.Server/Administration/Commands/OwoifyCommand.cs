using Content.Server.Speech.EntitySystems;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Random;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Fun)]
public sealed class OwoifyCommand : IConsoleCommand
{
    public string Command => "owoify";

    public string Description => "For when you need everything to be cat. Uses OwOAccent's formatting on the name and description of an entity.";

    public string Help => "owoify <id>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        var entityManager = IoCManager.Resolve<IEntityManager>();

        if (!int.TryParse(args[0], out var targetId))
        {
            shell.WriteLine(Loc.GetString("shell-argument-must-be-number"));
            return;
        }

        var eUid = new EntityUid(targetId);

        var meta = entityManager.GetComponent<MetaDataComponent>(eUid);

        var owoSys = entityManager.System<OwOAccentSystem>();
        var metaDataSys = entityManager.System<MetaDataSystem>();

        metaDataSys.SetEntityName(eUid, owoSys.Accentuate(meta.EntityName), meta);
        metaDataSys.SetEntityDescription(eUid, owoSys.Accentuate(meta.EntityDescription), meta);
    }
}
