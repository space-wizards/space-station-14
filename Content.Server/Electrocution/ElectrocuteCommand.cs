using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Electrocution;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Console;

namespace Content.Server.Electrocution;

[AdminCommand(AdminFlags.Fun)]
public sealed partial class ElectrocuteCommand : LocalizedEntityCommands
{
    [Dependency] private ElectrocutionSystem _electrocution = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    public override string Command => "electrocute";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is < 1 or > 3)
        {
            shell.WriteError(Loc.GetString($"shell-need-between-arguments",
                ("lower", 1),
                ("upper", 3)));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var uidNet) || !EntityManager.TryGetEntity(uidNet, out var uid) || !EntityManager.EntityExists(uid))
        {
            shell.WriteError(Loc.GetString($"shell-could-not-find-entity-with-uid", ("uid", args[0])));
            return;
        }

        if (!_statusEffects.CanAddStatusEffect(uid.Value, SharedElectrocutionSystem.ElectrocutionId))
        {
            shell.WriteError(Loc.GetString("cmd-electrocute-entity-cannot-be-electrocuted"));
            return;
        }

        if (args.Length < 2 || !int.TryParse(args[1], out var seconds))
            seconds = 10;

        if (args.Length < 3 || !int.TryParse(args[2], out var damage))
            damage = 10;

        _electrocution.TryDoElectrocution(uid.Value, null, damage, TimeSpan.FromSeconds(seconds), refresh: true, ignoreInsulation: true);
    }
}
