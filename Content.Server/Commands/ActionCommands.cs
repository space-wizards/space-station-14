using Content.Server.Administration;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Commands;

[AdminCommand(AdminFlags.Fun)]
internal sealed class UpgradeActionCommand : LocalizedEntityCommands
{
    [Dependency] private readonly ActionUpgradeSystem _actionUpgradeSystem = default!;

    public override string Command => "upgradeaction";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
        {
            shell.WriteLine(Loc.GetString("cmd-upgradeaction-need-one-argument"));
            return;
        }

        if (args.Length > 2)
        {
            shell.WriteLine(Loc.GetString("cmd-upgradeaction-max-two-arguments"));
            return;
        }

        var id = args[0];

        if (!NetEntity.TryParse(id, out var nuid))
        {
            shell.WriteLine(Loc.GetString("cmd-upgradeaction-incorrect-entityuid-format"));
            return;
        }

        if (!EntityManager.TryGetEntity(nuid, out var uid))
        {
            shell.WriteLine(Loc.GetString("cmd-upgradeaction-entity-does-not-exist"));
            return;
        }

        if (!EntityManager.TryGetComponent<ActionUpgradeComponent>(uid, out var actionUpgradeComponent))
        {
            shell.WriteLine(Loc.GetString("cmd-upgradeaction-entity-is-not-action"));
            return;
        }

        if (args.Length == 1)
        {
            if (!_actionUpgradeSystem.TryUpgradeAction(uid, out _, actionUpgradeComponent))
            {
                shell.WriteLine(Loc.GetString("cmd-upgradeaction-cannot-level-up"));
                return;
            }
        }

        if (args.Length == 2)
        {
            var levelArg = args[1];

            if (!int.TryParse(levelArg, out var level))
            {
                shell.WriteLine(Loc.GetString("cmd-upgradeaction-second-argument-not-number"));
                return;
            }

            if (level <= 0)
            {
                shell.WriteLine(Loc.GetString("cmd-upgradeaction-less-than-required-level"));
                return;
            }

            if (!_actionUpgradeSystem.TryUpgradeAction(uid, out _, actionUpgradeComponent, level))
                shell.WriteLine(Loc.GetString("cmd-upgradeaction-cannot-level-up"));
        }
    }
}
