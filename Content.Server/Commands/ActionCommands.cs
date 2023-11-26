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

        // var minds = IoCManager.Resolve<IEntityManager>().System<SharedMindSystem>();
        // var gameTicker = EntitySystem.Get<GameTicker>();
        // var suicideSystem = EntitySystem.Get<SuicideSystem>();

        var actionUpgrade = _entMan.EntitySysManager.GetEntitySystem<ActionUpgradeSystem>();

        var id = args[0];

        if (!EntityUid.TryParse(id, out var euid))
        {
            shell.WriteLine(Loc.GetString("upgradeaction-command-incorrect-entityuid-format"));
            return;
        }

        if (!_entMan.EntityExists(euid))
        {
            shell.WriteLine(Loc.GetString("upgradeaction-command-entityuid-does-not-exist"));
            return;
        }

        // TODO: Also check if it has the action upgrade component

        // TODO: If only one arg, increment level

        // TODO: If 2 args, check if 2nd arg is a number then upgrade action system can set level
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

            actionUpgrade.UpgradeAction(new EntityUid(123), level);
        }
    }
}
