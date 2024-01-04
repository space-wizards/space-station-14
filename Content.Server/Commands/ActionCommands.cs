using Content.Server.Administration;
using Content.Shared.Actions;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Commands;

[AdminCommand(AdminFlags.Fun)]
internal sealed class UpgradeActionCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    public string Command => "upgradeaction";
    public string Description => Loc.GetString("upgradeaction-command-description");
    public string Help => Loc.GetString("upgradeaction-command-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
        {
            shell.WriteLine(Loc.GetString("upgradeaction-command-need-one-argument"));
            return;
        }

        if (args.Length > 2)
        {
            shell.WriteLine(Loc.GetString("upgradeaction-command-max-two-arguments"));
            return;
        }

        var actionUpgrade = _entMan.EntitySysManager.GetEntitySystem<ActionUpgradeSystem>();
        var id = args[0];

        if (!NetEntity.TryParse(id, out var nuid))
        {
            shell.WriteLine(Loc.GetString("upgradeaction-command-incorrect-entityuid-format"));
            return;
        }

        if (!_entMan.TryGetEntity(nuid, out var uid))
        {
            shell.WriteLine(Loc.GetString("upgradeaction-command-entity-does-not-exist"));
            return;
        }

        if (!_entMan.TryGetComponent<ActionUpgradeComponent>(uid, out var actionUpgradeComponent))
        {
            shell.WriteLine(Loc.GetString("upgradeaction-command-entity-is-not-action"));
            return;
        }

        if (args.Length == 1)
        {
            if (!actionUpgrade.TryUpgradeAction(uid, actionUpgradeComponent))
            {
                shell.WriteLine(Loc.GetString("upgradeaction-command-cannot-level-up"));
                return;
            }
        }

        if (args.Length == 2)
        {
            var levelArg = args[1];

            if (!int.TryParse(levelArg, out var level))
            {
                shell.WriteLine(Loc.GetString("upgradeaction-command-second-argument-not-number"));
                return;
            }

            if (level <= 0)
            {
                shell.WriteLine(Loc.GetString("upgradeaction-command-less-than-required-level"));
                return;
            }

            if (!actionUpgrade.TryUpgradeAction(uid, actionUpgradeComponent, level))
                shell.WriteLine(Loc.GetString("upgradeaction-command-cannot-level-up"));
        }
    }
}
